using System;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Kurisu.AspNetCore.Cache;

public sealed class RedisLock : IDisposable, IAsyncDisposable
{
    private readonly IDatabase _db;
    private readonly string _lockKey;
    private readonly string _lockValue = Guid.NewGuid().ToString();
    private readonly TimeSpan _expiry;
    private readonly TimeSpan _interval;
    private bool _acquired;
    private CancellationTokenSource _cts;

    /// <summary>
    /// 构造 RedisLock 实例。
    /// </summary>
    /// <param name="db">Redis 数据库实例。</param>
    /// <param name="lockKey">锁的键名。</param>
    /// <param name="expiry">锁的过期时间，默认3秒。</param>
    /// <exception cref="ArgumentException">当 expiry 不是3的倍数时抛出。</exception>
    public RedisLock(IDatabase db, string lockKey, TimeSpan? expiry = null)
    {
        _db = db;
        _lockKey = lockKey;
        _expiry = expiry ?? TimeSpan.FromSeconds(3);

        if (_expiry.TotalSeconds % 3 != 0)
            throw new ArgumentException(nameof(expiry) + " 应该为3的倍数");

        _interval = TimeSpan.FromSeconds(_expiry.TotalSeconds / 3);
    }

    /// <summary>
    /// 当前锁是否已获取。
    /// </summary>
    public bool Acquired => _acquired;

    /// <summary>
    /// 异步尝试获取锁，并自动续期。
    /// </summary>
    /// <returns>返回自身实例。</returns>
    public async Task<RedisLock> LockAsync()
    {
        _acquired = await _db.StringSetAsync(_lockKey, _lockValue, _expiry, when: When.NotExists);
        if (_acquired)
        {
            _cts = new CancellationTokenSource();
            _ = StartRenewalAsync(_cts.Token);
        }

        return this;
    }

    /// <summary>
    /// 后台自动续期任务。
    /// </summary>
    /// <param name="token">取消令牌。</param>
    private async Task StartRenewalAsync(CancellationToken token)
    {
        try
        {
            while (_acquired && !token.IsCancellationRequested)
            {
                await Task.Delay(_interval, token);
                await _db.KeyExpireAsync(_lockKey, _expiry, when: ExpireWhen.Always);
            }
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        catch (Exception)
        {
            // 可记录日志
        }
    }

    /// <summary>
    /// 异步释放锁。
    /// </summary>
    /// <returns>异步任务。</returns>
    public async ValueTask DisposeAsync()
    {
        if (_acquired)
        {
            _cts?.Cancel();
            var getValue = await _db.StringGetAsync(_lockKey);
            if (getValue == _lockValue)
            {
                await _db.KeyDeleteAsync(_lockKey);
            }
        }

        _acquired = false;
        _cts?.Dispose();
    }

    /// <summary>
    /// 释放锁。
    /// </summary>
    public void Dispose()
    {
        if (_acquired)
        {
            _cts?.Cancel();
            var getValue = _db.StringGet(_lockKey);
            if (getValue == _lockValue)
            {
                _db.KeyDelete(_lockKey);
            }
        }

        _acquired = false;
        _cts?.Dispose();
    }
}