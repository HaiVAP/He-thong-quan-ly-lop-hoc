using EduHub.Contracts;
using EduHub.Materials.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EduHub.Materials.Application.Services;

public class MaterialsStatistics(MaterialsDbContext db) : IModuleStatistics
{
    public string ModuleName => "Materials";

    public async Task<IReadOnlyDictionary<string, long>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        return new Dictionary<string, long>
        {
            ["Materials"] = await db.Materials.AsNoTracking().LongCountAsync(cancellationToken),
            ["MaterialCategories"] = await db.MaterialCategories.AsNoTracking().LongCountAsync(cancellationToken)
        };
    }
}
