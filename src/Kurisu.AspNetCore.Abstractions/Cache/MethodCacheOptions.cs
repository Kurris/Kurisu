namespace Kurisu.AspNetCore.Abstractions.Cache;

public sealed class MethodCacheOptions
{
    /// <summary>必须为应用及环境设置独立前缀，例如 shop:production。</summary>
    public string KeyPrefix { get; set; } = "kurisu:method-cache";

    public IDictionary<string, MethodCachePolicy> Policies { get; } =
        new Dictionary<string, MethodCachePolicy>(StringComparer.Ordinal) { ["Default"] = new() };
}

public sealed class MethodCachePolicy
{
    public string Version { get; set; } = "1";
    public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(5);
    public bool CacheNull { get; set; } = true;
    public TimeSpan NullExpiry { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>TTL 向下随机浮动比例，范围 [0, 1)。</summary>
    public double ExpiryJitterRatio { get; set; } = 0.1;

    public TimeSpan LockWaitTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan LockPollInterval { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan LockExpiry { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>通过查询方法的 CancellationToken 协作取消；不提前释放仍在执行查询的锁。</summary>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public bool VaryByUser { get; set; } = true;
    public bool VaryByCulture { get; set; } = true;

    /// <summary>Redis 读取或获取锁失败时，在进程内同 Key 互斥下直查；不吞业务异常。</summary>
    public bool BypassOnCacheFailure { get; set; }

    /// <summary>分布式锁等待超时后允许直查。默认关闭，避免故障期间失控回源。</summary>
    public bool BypassOnLockTimeout { get; set; }
}