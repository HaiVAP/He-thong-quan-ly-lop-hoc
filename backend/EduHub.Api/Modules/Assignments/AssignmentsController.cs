using System.Security.Claims;
using EduHub.Api.Common.Exceptions;
using EduHub.Api.Common.Models;
using EduHub.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Api.Modules.Assignments;

[ApiController]
[Authorize]
public class AssignmentsController(EduHubDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<bool> CanAccessClassAsync(int classId) =>
        User.IsInRole("Admin") ||
        await db.Classes.AnyAsync(x => x.Id == classId && x.LecturerId == CurrentUserId) ||
        await db.ClassMembers.AnyAsync(x => x.ClassRoomId == classId && x.StudentId == CurrentUserId && x.Status == "Active");

    public record CreateAssignmentRequest(string Title, string Description, DateTime Deadline, bool AllowResubmit = true, int MaxAttempts = 2);
    public record SubmitAssignmentRequest(string? Content, string? FileUrl);
    public record GradeAssignmentRequest(decimal Score, string? Feedback);

    /// <summary>
    /// FR-19: Xem danh sách bài tập trong lớp học
    /// </summary>
    [HttpGet("api/classes/{classId:int}/assignments")]
    public async Task<IActionResult> ListByClass(int classId)
    {
        if (!await CanAccessClassAsync(classId))
            throw new ForbiddenException("Bạn không có quyền truy cập bài tập của lớp học này.");

        var assignments = await db.Assignments
            .AsNoTracking()
            .Where(x => x.ClassRoomId == classId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(ApiResponse.Ok(assignments));
    }

    /// <summary>
    /// FR-18: Giảng viên tạo bài tập mới cho lớp
    /// </summary>
    [HttpPost("api/classes/{classId:int}/assignments")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Create(int classId, [FromBody] CreateAssignmentRequest request)
    {
        var classRoom = await db.Classes.FindAsync(classId);
        if (classRoom is null)
            throw new NotFoundException($"Không tìm thấy lớp học có ID {classId}.");

        if (classRoom.LecturerId != CurrentUserId)
            throw new ForbiddenException("Bạn chỉ có thể tạo bài tập cho lớp học do mình phụ trách.");

        if (request.Deadline <= DateTime.UtcNow)
            throw new BadRequestException("Thời hạn nộp bài phải lớn hơn thời điểm hiện tại.");

        if (request.MaxAttempts < 1)
            throw new BadRequestException("Số lần nộp tối đa phải ít nhất là 1.");

        var assignment = new Assignment
        {
            ClassRoomId = classId,
            CreatedById = CurrentUserId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Deadline = request.Deadline,
            AllowResubmit = request.AllowResubmit,
            MaxAttempts = request.MaxAttempts
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok(assignment, "Tạo bài tập thành công."));
    }

    /// <summary>
    /// FR-20: Sinh viên nộp bài tập trực tuyến
    /// </summary>
    [HttpPost("api/assignments/{id:int}/submissions")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitAssignmentRequest request)
    {
        var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.Id == id);
        if (assignment is null)
            throw new NotFoundException($"Không tìm thấy bài tập có ID {id}.");

        var isEnrolled = await db.ClassMembers.AnyAsync(x =>
            x.ClassRoomId == assignment.ClassRoomId && x.StudentId == CurrentUserId && x.Status == "Active");

        if (!isEnrolled)
            throw new ForbiddenException("Bạn không phải thành viên hoạt động của lớp học này.");

        if (DateTime.UtcNow > assignment.Deadline)
            throw new BadRequestException("Đã quá thời hạn nộp bài tập.");

        var previousAttempts = await db.Submissions.CountAsync(x => x.AssignmentId == id && x.StudentId == CurrentUserId);

        if (previousAttempts > 0 && !assignment.AllowResubmit)
            throw new ConflictException("Bài tập này không cho phép nộp lại.");

        if (previousAttempts >= assignment.MaxAttempts)
            throw new ConflictException($"Bạn đã đạt giới hạn tối đa {assignment.MaxAttempts} lần nộp bài.");

        var submission = new Submission
        {
            AssignmentId = id,
            StudentId = CurrentUserId,
            AttemptNumber = previousAttempts + 1,
            Content = request.Content?.Trim(),
            FileUrl = request.FileUrl?.Trim(),
            SubmittedAt = DateTime.UtcNow
        };

        db.Submissions.Add(submission);
        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok(submission, "Nộp bài tập thành công."));
    }

    /// <summary>
    /// FR-21: Giảng viên xem danh sách bài nộp của sinh viên
    /// </summary>
    [HttpGet("api/assignments/{id:int}/submissions")]
    [Authorize(Roles = "Lecturer,Admin")]
    public async Task<IActionResult> GetSubmissions(int id)
    {
        var assignment = await db.Assignments.FindAsync(id);
        if (assignment is null)
            throw new NotFoundException($"Không tìm thấy bài tập có ID {id}.");

        if (!User.IsInRole("Admin") && assignment.CreatedById != CurrentUserId)
            throw new ForbiddenException("Bạn chỉ có thể xem bài nộp của bài tập do mình tạo.");

        var submissions = await db.Submissions
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.AssignmentId == id)
            .OrderByDescending(x => x.SubmittedAt)
            .Select(x => new
            {
                x.Id,
                x.StudentId,
                StudentName = x.Student.FullName,
                StudentEmail = x.Student.Email,
                x.AttemptNumber,
                x.Content,
                x.FileUrl,
                x.SubmittedAt,
                x.Score,
                x.Feedback,
                x.GradedAt
            })
            .ToListAsync();

        return Ok(ApiResponse.Ok(submissions));
    }

    /// <summary>
    /// FR-22: Giảng viên chấm điểm và nhận xét bài nộp
    /// </summary>
    [HttpPut("api/submissions/{id:int}/grade")]
    [Authorize(Roles = "Lecturer")]
    public async Task<IActionResult> Grade(int id, [FromBody] GradeAssignmentRequest request)
    {
        var submission = await db.Submissions
            .Include(x => x.Assignment)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (submission is null)
            throw new NotFoundException($"Không tìm thấy bài nộp có ID {id}.");

        if (submission.Assignment.CreatedById != CurrentUserId)
            throw new ForbiddenException("Bạn chỉ có thể chấm bài tập do mình phụ trách.");

        if (request.Score is < 0 or > 100)
            throw new BadRequestException("Thang điểm hợp lệ từ 0 đến 100.");

        submission.Score = request.Score;
        submission.Feedback = request.Feedback?.Trim();
        submission.GraderId = CurrentUserId;
        submission.GradedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(ApiResponse.Ok(submission, "Chấm điểm bài nộp thành công."));
    }
}
