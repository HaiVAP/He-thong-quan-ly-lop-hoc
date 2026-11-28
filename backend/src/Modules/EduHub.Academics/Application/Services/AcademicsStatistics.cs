using EduHub.Academics.Infrastructure;
using EduHub.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Academics.Application.Services;

public class AcademicsStatistics(AcademicsDbContext db) : IModuleStatistics
{
    public string ModuleName => "Academics";

    public async Task<IReadOnlyDictionary<string, long>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        return new Dictionary<string, long>
        {
            ["Departments"] = await db.Departments.AsNoTracking().LongCountAsync(cancellationToken),
            ["Classes"] = await db.Classes.AsNoTracking().LongCountAsync(cancellationToken),
            ["ClassMembers"] = await db.ClassMembers.AsNoTracking().LongCountAsync(cancellationToken)
        };
    }
}
