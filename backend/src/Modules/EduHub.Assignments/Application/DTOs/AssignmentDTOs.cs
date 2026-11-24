namespace EduHub.Assignments.Application.DTOs;

public record CreateAssignmentRequest(string Title, string Description, DateTime Deadline, bool AllowResubmit = true, int MaxAttempts = 2);
public record SubmitAssignmentRequest(string? Content, string? FileUrl);
public record GradeAssignmentRequest(decimal Score, string? Feedback);

public record SubmissionResponseDto(
    int Id,
    int StudentId,
    string StudentName,
    string StudentEmail,
    int AttemptNumber,
    string? Content,
    string? FileUrl,
    DateTime SubmittedAt,
    decimal? Score,
    string? Feedback,
    DateTime? GradedAt
);

