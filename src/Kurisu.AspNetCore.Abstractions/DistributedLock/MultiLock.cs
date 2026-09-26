using Kurisu.AspNetCore.Abstractions.Result;

namespace Kurisu.AspNetCore.Abstractions.DistributedLock;

/// <summary>
/// 多锁持有器，用于同时持有多个分布式锁并在释放时按相反顺序解锁。
/// </summary>
public class MultiLock : IAsyncDisposable
{
    private readonly Stack<ILockHandler> _handlers;

    private MultiLock(Stack<ILockHandler> handlers)
    {
        _handlers = handlers;
    }

    /// <summary>
    /// 从参数值解析锁Key、依次获取多个分布式锁，获取失败时自动释放已持有的锁。
    /// </summary>
    /// <param name="lockable">锁提供者</param>
    /// <param name="scene">锁场景名</param>
    /// <param name="keys">表达式解析后的锁 Key 集合。</param>
    /// <param name="options">锁获取选项</param>
    /// <param name="tips">获取失败时的提示信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>多锁持有器实例</returns>
    public static async Task<MultiLock> AcquireAsync(
        ILockable lockable,
        string scene,
        IEnumerable<string> keys,
        DistributedLockAcquisitionOptions options,
        string tips,
        CancellationToken cancellationToken = default)
    {
        var lockKeys = keys?.Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal)
            .Select(k => $"Locker:{scene}:{k}").ToArray();
        if (lockKeys == null || lockKeys.Length == 0)
            throw new ArgumentException("必须提供至少一个有效的锁定 Key。", nameof(keys));

        var handlers = new Stack<ILockHandler>();
        try
        {
            foreach (var key in lockKeys)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var handler = await lockable.LockAsync(key, options, cancellationToken);
                handlers.Push(handler);
                handler.Acquired.ThrowIfFalse(tips);
            }

            return new MultiLock(handlers);
        }
        catch
        {
            while (handlers.TryPop(out var handler))
            {
                await handler.DisposeAsync();
            }

            throw;
        }
    }

    /// <summary>
    /// 按相反顺序释放所有持有的锁。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        while (_handlers.TryPop(out var handler))
        {
            await handler.DisposeAsync();
        }
    }
}
