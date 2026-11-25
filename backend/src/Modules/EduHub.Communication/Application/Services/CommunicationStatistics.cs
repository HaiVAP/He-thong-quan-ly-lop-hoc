using EduHub.Communication.Infrastructure;
using EduHub.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Communication.Application.Services;

public class CommunicationStatistics(CommunicationDbContext db) : IModuleStatistics
{
    public string ModuleName => "Communication";

    public async Task<IReadOnlyDictionary<string, long>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        return new Dictionary<string, long>
        {
            ["Announcements"] = await db.Announcements.AsNoTracking().LongCountAsync(cancellationToken)
        };
    }
}
