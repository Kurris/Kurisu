namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 分布式锁重试策略。
/// </summary>
public interface IDistributedLockRetryStrategy
{
    /// <summary>
    /// 当前失败后是否继续重试。
    /// </summary>
    /// <param name="attempt">当前失败次数（从 1 开始）。</param>
    Task<bool> ShouldRetryAsync(int attempt, CancellationToken cancellationToken = default);

    /// <summary>
    /// 重试前等待。
    /// </summary>
    /// <param name="attempt">当前失败次数（从 1 开始）。</param>
    Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken = default);
}
