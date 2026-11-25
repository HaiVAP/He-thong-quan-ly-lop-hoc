using EduHub.Contracts;
using EduHub.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Statistics.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize(Roles = "Admin")]
public class StatisticsController(IEnumerable<IModuleStatistics> moduleStatistics) : ControllerBase
{
    /// <summary>
    /// FR-31: Admin xem thống kê tổng quan. Controller chỉ gom số liệu từng module đã đăng ký.
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var modules = new List<ModuleStatisticsResult>();
        var totals = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var module in moduleStatistics)
        {
            var metrics = await module.GetMetricsAsync(cancellationToken);
            modules.Add(new ModuleStatisticsResult(module.ModuleName, metrics));

            foreach (var (key, value) in metrics)
                totals[key] = totals.GetValueOrDefault(key) + value;
        }

        return Ok(ApiResponse.Ok(new StatisticsOverviewResponse(modules, totals)));
    }
}

public record ModuleStatisticsResult(string Module, IReadOnlyDictionary<string, long> Metrics);

public record StatisticsOverviewResponse(
    IReadOnlyList<ModuleStatisticsResult> Modules,
    IReadOnlyDictionary<string, long> Totals);
