using Microsoft.Extensions.DependencyInjection;

namespace EduHub.Statistics;

/// <summary>
/// Statistics không có DbContext. Số liệu đến từ các IModuleStatistics do module khác đăng ký.
/// </summary>
public static class StatisticsModuleExtensions
{
    public static IServiceCollection AddStatisticsModule(this IServiceCollection services) => services;
}
