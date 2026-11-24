using EduHub.Assignments.Application.DTOs;
using EduHub.Assignments.Domain;
using EduHub.Assignments.Infrastructure;
using EduHub.Contracts;
using EduHub.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Assignments.Application.Services;

public class AssignmentService(AssignmentsDbContext db, IClassAccess classAccess, IUserDirectory userDirectory) : IAssignmentService
{
    public async Task<List<Assignment>> ListByClassAsync(int classId, int currentUserId, bool isAdmin)
    {
        if (!await classAccess.CanAccessClassAsync(classId, currentUserId, isAdmin))
            throw new ForbiddenException("Bạn không có quyền truy cập bài tập của lớp học này.");

        var assignments = await db.Assignments
            .AsNoTracking()
            .Where(x => x.ClassRoomId == classId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return assignments;
    }

    public async Task<Assignment> CreateAsync(int classId, int currentUserId, bool isAdmin, CreateAssignmentRequest request)
    {
        var classSummary = await classAccess.GetClassSummaryAsync(classId);
        if (classSummary is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {classId}.");

        var isLecturer = await classAccess.IsLecturerOfClassAsync(classId, currentUserId);
        if (!isLecturer && !isAdmin)
            throw new ForbiddenException("Bạn chỉ có thể tạo bài tập cho lớp học do mình phụ trách.");

        if (request.Deadline <= DateTime.UtcNow)
            throw new BadRequestException("Thời hạn nộp bài phải lớn hơn thời điểm hiện tại.");

        if (request.MaxAttempts < 1)
            throw new BadRequestException("Số lần nộp tối đa phải ít nhất là 1.");

        var assignment = new Assignment
        {
            ClassRoomId = classId,
            CreatedById = currentUserId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Deadline = request.Deadline,
            AllowResubmit = request.AllowResubmit,
            MaxAttempts = request.MaxAttempts
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        return assignment;
    }

    public async Task<Submission> SubmitAsync(int assignmentId, int currentUserId, SubmitAssignmentRequest request)
    {
        var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.Id == assignmentId);
        if (assignment is null)
            throw new NotFoundException($"Không tìm thấy bài tập có ID {assignmentId}.");

        var isEnrolled = await classAccess.IsMemberOfClassAsync(assignment.ClassRoomId, currentUserId);
        if (!isEnrolled)
            throw new ForbiddenException("Bạn không phải thành viên hoạt động của lớp học này.");

        if (DateTime.UtcNow > assignment.Deadline)
            throw new BadRequestException("Đã quá thời hạn nộp bài tập.");

        var previousAttempts = await db.Submissions.CountAsync(x => x.AssignmentId == assignmentId && x.StudentId == currentUserId);

        if (previousAttempts > 0 && !assignment.AllowResubmit)
            throw new ConflictException("Bài tập này không cho phép nộp lại.");

        if (previousAttempts >= assignment.MaxAttempts)
            throw new ConflictException($"Bạn đã đạt giới hạn tối đa {assignment.MaxAttempts} lần nộp bài.");

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = currentUserId,
            AttemptNumber = previousAttempts + 1,
            Content = request.Content?.Trim(),
            FileUrl = request.FileUrl?.Trim(),
            SubmittedAt = DateTime.UtcNow
        };

        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        return submission;
    }

    public async Task<List<SubmissionResponseDto>> GetSubmissionsAsync(int assignmentId, int currentUserId, bool isAdmin)
    {
        var assignment = await db.Assignments.FindAsync(assignmentId);
        if (assignment is null)
            throw new NotFoundException($"Không tìm thấy bài tập có ID {assignmentId}.");

        var isLecturer = await classAccess.IsLecturerOfClassAsync(assignment.ClassRoomId, currentUserId);
        if (!isAdmin && assignment.CreatedById != currentUserId && !isLecturer)
            throw new ForbiddenException("Bạn chỉ có thể xem bài nộp của bài tập do mình phụ trách.");

        var submissions = await db.Submissions
            .AsNoTracking()
            .Where(x => x.AssignmentId == assignmentId)
            .OrderByDescending(x => x.SubmittedAt)
            .ToListAsync();

        var studentIds = submissions.Select(x => x.StudentId).Distinct().ToList();
        var studentMap = await userDirectory.GetByIdsAsync(studentIds);

        var result = submissions.Select(x =>
        {
            studentMap.TryGetValue(x.StudentId, out var student);
            return new SubmissionResponseDto(
                x.Id,
                x.StudentId,
                student?.FullName ?? "Sinh viên",
                student?.Email ?? "",
                x.AttemptNumber,
                x.Content,
                x.FileUrl,
                x.SubmittedAt,
                x.Score,
                x.Feedback,
                x.GradedAt
            );
        }).ToList();

        return result;
    }

    public async Task<Submission> GradeAsync(int submissionId, int currentUserId, bool isAdmin, GradeAssignmentRequest request)
    {
        var submission = await db.Submissions
            .Include(x => x.Assignment)
            .SingleOrDefaultAsync(x => x.Id == submissionId);

        if (submission is null)
            throw new NotFoundException($"Không tìm thấy bài nộp có ID {submissionId}.");

        var isLecturer = await classAccess.IsLecturerOfClassAsync(submission.Assignment.ClassRoomId, currentUserId);
        if (!isAdmin && submission.Assignment.CreatedById != currentUserId && !isLecturer)
            throw new ForbiddenException("Bạn chỉ có thể chấm bài tập do mình phụ trách.");

        if (request.Score is < 0 or > 100)
            throw new BadRequestException("Thang điểm hợp lệ từ 0 đến 100.");

        submission.Score = request.Score;
        submission.Feedback = request.Feedback?.Trim();
        submission.GraderId = currentUserId;
        submission.GradedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return submission;
    }
}

