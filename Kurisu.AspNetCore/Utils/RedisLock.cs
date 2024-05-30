using System.Threading.Tasks;
using System;
using StackExchange.Redis;

namespace Kurisu.AspNetCore.Utils;

/// <summary>
/// redis lock
/// </summary>
public sealed class RedisLock : IDisposable, IAsyncDisposable
{
    private readonly IDatabase _db;

    /// <summary>
    /// 锁标识
    /// </summary>
    private bool _acquired;

    private readonly string _lockKey;
    private readonly string _lockValue = Guid.NewGuid().ToString();
    private readonly TimeSpan? _expiry;
    private readonly int _interval;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="lockKey"></param>
    /// <param name="expiry"></param>
    /// <exception cref="ArgumentException"></exception>
    public RedisLock(IDatabase db, string lockKey, TimeSpan? expiry = null)
    {
        _db = db;
        _lockKey = lockKey;
        _expiry = expiry;
        if (!_expiry.HasValue)
        {
            _expiry = TimeSpan.FromSeconds(3);
        }
        else
        {
            var interval = _expiry.Value.TotalSeconds % 3;
            if (interval != 0)
            {
                throw new ArgumentException(nameof(expiry) + "应该为3的倍数");
            }
        }

        _interval = (_expiry.Value / 3).Seconds;
    }

    /// <summary>
    /// 锁标识
    /// </summary>
    public bool Acquired => _acquired;

    /// <summary>
    /// lock
    /// </summary>
    /// <returns></returns>
    public async Task<RedisLock> LockAsync()
    {
        _acquired = await _db.StringSetAsync(_lockKey, _lockValue, _expiry, when: When.NotExists);
        if (_acquired)
        {
            _ = StartRenewalAsync();
        }

        return this;
    }

    /// <summary>
    /// lock
    /// </summary>
    /// <returns></returns>
    public RedisLock Lock()
    {
        _acquired = _db.StringSet(_lockKey, _lockValue, _expiry, when: When.NotExists);
        if (_acquired)
        {
            _ = StartRenewalAsync();
        }

        return this;
    }

    /// <summary>
    /// 续
    /// </summary>
    /// <returns></returns>
    private async Task StartRenewalAsync()
    {
        while (_acquired)
        {
            await Task.Delay(_interval);
            await _db.KeyExpireAsync(_lockKey, _expiry, when: ExpireWhen.Always);
        }
    }

    /// <summary>
    /// 解锁
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        //如果获取lock成功
        if (_acquired)
        {
            //判断是否为当前value
            var getValue = await _db.StringGetAsync(_lockKey);
            if (getValue == _lockValue)
            {
                //移除string locker
                await _db.KeyDeleteAsync(_lockKey);
            }
        }

        _acquired = false;
    }

    /// <summary>
    /// 解锁
    /// </summary>
    public void Dispose()
    {
        //如果获取lock成功
        if (_acquired)
        {
            //判断是否为当前value
            var getValue = _db.StringGet(_lockKey);
            if (getValue == _lockValue)
            {
                //移除string locker
                _db.KeyDelete(_lockKey);
            }
        }

        _acquired = false;
    }
}