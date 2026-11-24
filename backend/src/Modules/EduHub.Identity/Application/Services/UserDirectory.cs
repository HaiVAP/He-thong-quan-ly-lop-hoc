using EduHub.Contracts;
using EduHub.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Identity.Application.Services;

public class UserDirectory(IdentityDbContext db) : IUserDirectory
{
    public async Task<UserSummaryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserSummaryDto(u.Id, u.FullName, u.Email, u.Role, u.Status, u.AvatarUrl))
            .SingleOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task<Dictionary<int, UserSummaryDto>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<int, UserSummaryDto>();

        var users = await db.Users.AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .Select(u => new UserSummaryDto(u.Id, u.FullName, u.Email, u.Role, u.Status, u.AvatarUrl))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return users;
    }
}

