namespace EduHub.Contracts.Events;

public record StudentJoinedClassEvent(
    int ClassRoomId,
    string ClassName,
    int StudentId,
    int LecturerId,
    DateTime JoinedAt);

public record AssignmentCreatedEvent(
    int AssignmentId,
    int ClassRoomId,
    int CreatedById,
    string Title,
    DateTime Deadline,
    DateTime CreatedAt);

public record SubmissionSubmittedEvent(
    int SubmissionId,
    int AssignmentId,
    string AssignmentTitle,
    int ClassRoomId,
    int StudentId,
    int AttemptNumber,
    DateTime SubmittedAt);

public record SubmissionGradedEvent(
    int SubmissionId,
    int AssignmentId,
    string AssignmentTitle,
    int ClassRoomId,
    int StudentId,
    int GraderId,
    decimal Score,
    DateTime GradedAt);

public record MaterialUploadedEvent(
    int MaterialId,
    int ClassRoomId,
    int CreatedById,
    string Title,
    string FileName,
    DateTime UploadedAt);

