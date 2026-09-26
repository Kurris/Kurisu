namespace Kurisu.AspNetCore.Abstractions.Cache;

public sealed class MethodCacheOptions
{
    /// <summary>必须为应用及环境设置独立前缀，例如 shop:production。</summary>
    public string KeyPrefix { get; set; } = "kurisu:method-cache";

    public IDictionary<string, MethodCachePolicy> Policies { get; } = new Dictionary<string, MethodCachePolicy>(StringComparer.Ordinal)
    {
        ["Default"] = new()
    };
}

/// <summary>方法查询缓存策略，控制缓存版本、有效期和加载超时。</summary>
public sealed class MethodCachePolicy
{
    /// <summary>缓存版本，参与默认 Key 生成。默认 "1"，不能为空；结果结构变更时可修改版本以隔离旧缓存。</summary>
    public string Version { get; set; } = "1";

    /// <summary>非 null 结果的缓存有效期，默认 30 分钟，必须大于零。实际有效期由内部固定按最多 10% 向下随机浮动。</summary>
    public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>等待进程内互斥及分布式锁的总时限，默认 10 秒，不包含获得锁后的业务查询耗时。</summary>
    /// <remarks>必须大于零且不超过 int.MaxValue 毫秒。进程内互斥或分布式锁等待超时均抛出 TimeoutException，不直接回源查询。</remarks>
    public TimeSpan LockWaitTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>分布式锁未获取时，再次读取缓存并尝试获取锁之前的等待间隔，默认 100 毫秒。</summary>
    /// <remarks>必须大于零且不超过 int.MaxValue 毫秒。</remarks>
    public TimeSpan LockPollInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>分布式加载锁的租约有效期，默认 6 秒。持锁期间自动续租，不代表查询执行时限。</summary>
    /// <remarks>必须大于零且不超过 int.MaxValue 毫秒。</remarks>
    public TimeSpan LockExpiry { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>业务查询的协作取消时限，默认 30 秒。通过替换查询方法的 CancellationToken 参数传递取消信号。</summary>
    /// <remarks>必须大于零且不超过 int.MaxValue 毫秒。方法没有取消参数或不响应取消时仍等待其完成，不提前释放锁；检测到超时后不回填缓存。</remarks>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);

}
