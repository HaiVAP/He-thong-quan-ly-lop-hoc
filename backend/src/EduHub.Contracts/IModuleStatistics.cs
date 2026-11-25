namespace EduHub.Contracts;

/// <summary>
/// Mỗi module tự trả số liệu của mình. Statistics chỉ gom các implementation đã đăng ký.
/// </summary>
public interface IModuleStatistics
{
    string ModuleName { get; }

    Task<IReadOnlyDictionary<string, long>> GetMetricsAsync(CancellationToken cancellationToken = default);
}
