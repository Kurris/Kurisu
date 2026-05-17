using Kurisu.AspNetCore.Abstractions.Result;

namespace Kurisu.AspNetCore.Abstractions.Cache;

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
    /// <param name="parameterValue">方法参数值（string / ITryLockKey / ITryLockKeys）</param>
    /// <param name="parameterName">参数名</param>
    /// <param name="parameterIndex">参数索引</param>
    /// <param name="options">锁获取选项</param>
    /// <param name="tips">获取失败时的提示信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>多锁持有器实例</returns>
    public static async Task<MultiLock> AcquireAsync(
        ILockable lockable,
        string scene,
        object parameterValue,
        string parameterName,
        int parameterIndex,
        DistributedLockAcquisitionOptions options,
        string tips,
        CancellationToken cancellationToken = default)
    {
        var lockKeys = ResolveLockKeys(parameterValue, parameterName, parameterIndex)
            .Select(k => $"Locker:{scene}:{k}");

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

    private static string[] ResolveLockKeys(object value, string parameterName, int parameterIndex)
    {
        IEnumerable<string> keys = value switch
        {
            string s => [s],
            ITryLockKey k => [k.GetKey()],
            ITryLockKeys k => k.GetKeys(),
            _ => throw new ArgumentException($"方法第{parameterIndex}个参数必须为string类型,或者实现{nameof(ITryLockKey)}/{nameof(ITryLockKeys)}接口.")
        };

        var lockKeys = keys?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() ?? Array.Empty<string>();
        if (lockKeys.Length == 0)
        {
            throw new ArgumentException("必须提供至少一个有效的锁定Key.", parameterName);
        }

        return lockKeys;
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
