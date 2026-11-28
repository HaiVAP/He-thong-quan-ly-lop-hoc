using EduHub.Assignments.Infrastructure;
using EduHub.Contracts;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Assignments.Application.Services;

public class AssignmentsStatistics(AssignmentsDbContext db) : IModuleStatistics
{
    public string ModuleName => "Assignments";

    public async Task<IReadOnlyDictionary<string, long>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        return new Dictionary<string, long>
        {
            ["Assignments"] = await db.Assignments.AsNoTracking().LongCountAsync(cancellationToken),
            ["Submissions"] = await db.Submissions.AsNoTracking().LongCountAsync(cancellationToken)
        };
    }
}
