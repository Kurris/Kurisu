using System;
using System.Text;

namespace Kurisu.AspNetCore.Utils;

/// <summary>
/// 雪花id帮助类
/// </summary>
public sealed class SnowFlakeHelper
{
    private static readonly object _lock = new();
    private static SnowFlakeHelper _instance;
    private static readonly DateTime _jan1St1970 = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="datacenterId"></param>
    /// <param name="workerId"></param>
    /// <returns></returns>
    public static SnowFlakeHelper Initialize(long datacenterId = 1, long workerId = 1)
    {
        // ReSharper disable once InvertIf
        //双if 加锁
        if (_instance == null)
        {
            lock (_lock)
            {
                // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
                if (_instance == null)
                {
                    _instance = new SnowFlakeHelper(datacenterId, workerId);
                }
            }
        }

        return _instance;
    }

    /// <summary>
    /// 实例
    /// </summary>
    public static SnowFlakeHelper Instance => Initialize();

    // 开始时间截((new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)-Jan1st1970).TotalMilliseconds)
    private const long _twepoch = 1577836800000L;

    // 机器id所占的位数
    private const int _workerIdBits = 5;

    // 数据标识id所占的位数
    private const int _datacenterIdBits = 5;

    // 支持的最大机器id，结果是31 (这个移位算法可以很快的计算出几位二进制数所能表示的最大十进制数)
    private const long _maxWorkerId = -1L ^ -1L << _workerIdBits;

    // 支持的最大数据标识id，结果是31
    private const long _maxDatacenterId = -1L ^ -1L << _datacenterIdBits;

    // 序列在id中占的位数
    private const int _sequenceBits = 12;

    // 数据标识id向左移17位(12+5)
    private const int _datacenterIdShift = _sequenceBits + _workerIdBits;

    // 机器ID向左移12位
    private const int _workerIdShift = _sequenceBits;

    // 时间截向左移22位(5+5+12)
    private const int _timestampLeftShift = _sequenceBits + _workerIdBits + _datacenterIdBits;

    // 生成序列的掩码，这里为4095 (0b111111111111=0xfff=4095)
    private const long _sequenceMask = -1L ^ -1L << _sequenceBits;

    // 数据中心ID(0~31)
    private long _datacenterId { get; }

    // 工作机器ID(0~31)
    private long _workerId { get; }

    // 毫秒内序列(0~4095)
    private long _sequence { get; set; }

    // 上次生成ID的时间截
    private long _lastTimestamp { get; set; }


    /// <summary>
    /// 雪花ID
    /// </summary>
    /// <param name="datacenterId">数据中心ID</param>
    /// <param name="workerId">工作机器ID</param>
    private SnowFlakeHelper(long datacenterId, long workerId)
    {
        if (datacenterId is > _maxDatacenterId or < 0)
        {
            throw new Exception($"datacenter Id can't be greater than {_maxDatacenterId} or less than 0");
        }

        if (workerId is > _maxWorkerId or < 0)
        {
            throw new Exception($"worker Id can't be greater than {_maxWorkerId} or less than 0");
        }

        _workerId = workerId;
        _datacenterId = datacenterId;
        _sequence = 0L;
        _lastTimestamp = -1L;
    }

    /// <summary>
    /// 获得下一个ID
    /// </summary>
    /// <returns></returns>
    public long NextId()
    {
        lock (this)
        {
            var timestamp = GetCurrentTimestamp();
            if (timestamp > _lastTimestamp) //时间戳改变，毫秒内序列重置
            {
                _sequence = 0L;
            }
            else if (timestamp == _lastTimestamp) //如果是同一时间生成的，则进行毫秒内序列
            {
                _sequence = _sequence + 1 & _sequenceMask;
                if (_sequence == 0) //毫秒内序列溢出
                {
                    timestamp = GetNextTimestamp(_lastTimestamp); //阻塞到下一个毫秒,获得新的时间戳
                }
            }
            else //当前时间小于上一次ID生成的时间戳，证明系统时钟被回拨，此时需要做回拨处理
            {
                _sequence = _sequence + 1 & _sequenceMask;
                if (_sequence > 0)
                {
                    timestamp = _lastTimestamp; //停留在最后一次时间戳上，等待系统时间追上后即完全度过了时钟回拨问题。
                }
                else //毫秒内序列溢出
                {
                    timestamp = _lastTimestamp + 1; //直接进位到下一个毫秒
                }
                //throw new Exception(string.Format("Clock moved backwards.  Refusing to generate id for {0} milliseconds", lastTimestamp - timestamp));
            }

            _lastTimestamp = timestamp; //上次生成ID的时间截

            //移位并通过或运算拼到一起组成64位的ID
            return timestamp - _twepoch << _timestampLeftShift
                     | _datacenterId << _datacenterIdShift
                     | _workerId << _workerIdShift
                     | _sequence;
        }
    }

    /// <summary>
    /// 解析雪花ID
    /// </summary>
    /// <returns></returns>
    public static string AnalyzeId(long id)
    {
        var sb = new StringBuilder();

        var timestamp = id >> _timestampLeftShift;
        var time = _jan1St1970.AddMilliseconds(timestamp + _twepoch);
        sb.Append(time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss:fff"));

        var datacenterId = (id ^ timestamp << _timestampLeftShift) >> _datacenterIdShift;
        sb.Append("_" + datacenterId);

        var workerId = (id ^ (timestamp << _timestampLeftShift | datacenterId << _datacenterIdShift)) >> _workerIdShift;
        sb.Append("_" + workerId);

        var sequence = id & _sequenceMask;
        sb.Append("_" + sequence);

        return sb.ToString();
    }

    /// <summary>
    /// 阻塞到下一个毫秒，直到获得新的时间戳
    /// </summary>
    /// <param name="lastTimestamp">上次生成ID的时间截</param>
    /// <returns>当前时间戳</returns>
    private static long GetNextTimestamp(long lastTimestamp)
    {
        var timestamp = GetCurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            timestamp = GetCurrentTimestamp();
        }

        return timestamp;
    }

    /// <summary>
    /// 获取当前时间戳
    /// </summary>
    /// <returns></returns>
    private static long GetCurrentTimestamp()
    {
        return (long)(DateTime.UtcNow - _jan1St1970).TotalMilliseconds;
    }
}