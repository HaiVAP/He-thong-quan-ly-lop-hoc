namespace EduHub.Contracts;

public record ClassSummaryDto(
    int Id,
    string Name,
    string? Description,
    int DepartmentId,
    string? DepartmentName,
    int LecturerId,
    string Status
);

public interface IClassAccess
{
    Task<bool> IsLecturerOfClassAsync(int classId, int userId, CancellationToken cancellationToken = default);
    Task<bool> IsMemberOfClassAsync(int classId, int userId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessClassAsync(int classId, int userId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<ClassSummaryDto?> GetClassSummaryAsync(int classId, CancellationToken cancellationToken = default);
}

