using EduHub.Communication.Domain;
using EduHub.Communication.Infrastructure;
using EduHub.Contracts;
using EduHub.Contracts.Events;
using EduHub.Shared.Events;

namespace EduHub.Communication.Application.EventHandlers;

public class StudentJoinedClassEventHandler(
    CommunicationDbContext db,
    IUserDirectory userDirectory) : IEventHandler<StudentJoinedClassEvent>
{
    public async Task HandleAsync(StudentJoinedClassEvent @event, CancellationToken cancellationToken = default)
    {
        var student = await userDirectory.GetByIdAsync(@event.StudentId, cancellationToken);
        var studentName = student?.FullName ?? $"Học viên #{@event.StudentId}";

        db.Announcements.Add(new Announcement
        {
            ClassRoomId = @event.ClassRoomId,
            AuthorId = @event.LecturerId,
            Title = $"Thành viên mới tham gia lớp {@event.ClassName}",
            Content = $"Học viên {studentName} vừa tham gia lớp học.",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class AssignmentCreatedEventHandler(
    CommunicationDbContext db) : IEventHandler<AssignmentCreatedEvent>
{
    public async Task HandleAsync(AssignmentCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        db.Announcements.Add(new Announcement
        {
            ClassRoomId = @event.ClassRoomId,
            AuthorId = @event.CreatedById,
            Title = $"Bài tập mới: {@event.Title}",
            Content = $"Giảng viên vừa giao bài tập \"{@event.Title}\" (Hạn nộp: {@event.Deadline:dd/MM/yyyy HH:mm} UTC).",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class SubmissionSubmittedEventHandler(
    CommunicationDbContext db,
    IClassAccess classAccess,
    IUserDirectory userDirectory) : IEventHandler<SubmissionSubmittedEvent>
{
    public async Task HandleAsync(SubmissionSubmittedEvent @event, CancellationToken cancellationToken = default)
    {
        var classSummary = await classAccess.GetClassSummaryAsync(@event.ClassRoomId, cancellationToken);
        var student = await userDirectory.GetByIdAsync(@event.StudentId, cancellationToken);
        var studentName = student?.FullName ?? $"Học viên #{@event.StudentId}";

        db.Announcements.Add(new Announcement
        {
            ClassRoomId = @event.ClassRoomId,
            AuthorId = classSummary?.LecturerId ?? @event.StudentId,
            Title = $"[Báo giảng viên] Học viên {studentName} vừa nộp bài: {@event.AssignmentTitle}",
            Content = $"Học viên {studentName} đã nộp bài tập \"{@event.AssignmentTitle}\" (Lần nộp thứ {@event.AttemptNumber}).",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class SubmissionGradedEventHandler(
    CommunicationDbContext db,
    IUserDirectory userDirectory) : IEventHandler<SubmissionGradedEvent>
{
    public async Task HandleAsync(SubmissionGradedEvent @event, CancellationToken cancellationToken = default)
    {
        var student = await userDirectory.GetByIdAsync(@event.StudentId, cancellationToken);
        var studentName = student?.FullName ?? $"Học viên #{@event.StudentId}";

        db.Announcements.Add(new Announcement
        {
            ClassRoomId = @event.ClassRoomId,
            AuthorId = @event.GraderId,
            Title = $"[Thông báo điểm] Bài tập \"{@event.AssignmentTitle}\" đã được chấm",
            Content = $"Bài nộp của học viên {studentName} cho bài tập \"{@event.AssignmentTitle}\" đã được chấm điểm: {@event.Score}.",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class MaterialUploadedEventHandler(
    CommunicationDbContext db) : IEventHandler<MaterialUploadedEvent>
{
    public async Task HandleAsync(MaterialUploadedEvent @event, CancellationToken cancellationToken = default)
    {
        db.Announcements.Add(new Announcement
        {
            ClassRoomId = @event.ClassRoomId,
            AuthorId = @event.CreatedById,
            Title = $"Tài liệu mới: {@event.Title}",
            Content = $"Giảng viên vừa tải lên tài liệu mới \"{@event.Title}\" ({@event.FileName}).",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}

