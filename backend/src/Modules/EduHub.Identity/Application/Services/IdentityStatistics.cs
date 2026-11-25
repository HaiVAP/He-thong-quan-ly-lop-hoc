using EduHub.Contracts;
using EduHub.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Identity.Application.Services;

public class IdentityStatistics(IdentityDbContext db) : IModuleStatistics
{
    public string ModuleName => "Identity";

    public async Task<IReadOnlyDictionary<string, long>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var users = db.Users.AsNoTracking();

        return new Dictionary<string, long>
        {
            ["Users"] = await users.LongCountAsync(cancellationToken),
            ["Lecturers"] = await users.LongCountAsync(x => x.Role == "Lecturer", cancellationToken),
            ["Students"] = await users.LongCountAsync(x => x.Role == "Student", cancellationToken)
        };
    }
}
