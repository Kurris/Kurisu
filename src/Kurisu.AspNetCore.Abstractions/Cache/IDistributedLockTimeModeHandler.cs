namespace Kurisu.AspNetCore.Abstractions.Cache;

/// <summary>
/// 锁时间模式处理器。
/// </summary>
public interface IDistributedLockTimeModeHandler
{
    /// <summary>
    /// 解析模式配置。
    /// </summary>
    /// <returns></returns>
    DistributedLockTimeSettings Resolve();
}
