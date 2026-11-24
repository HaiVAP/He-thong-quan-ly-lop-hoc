namespace EduHub.Contracts;

public record UserSummaryDto(
    int Id,
    string FullName,
    string Email,
    string Role,
    string Status,
    string? AvatarUrl = null
);

public interface IUserDirectory
{
    Task<UserSummaryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Dictionary<int, UserSummaryDto>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}

