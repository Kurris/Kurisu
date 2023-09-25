using System.Text;

namespace Kurisu.Utils;

public sealed class SnowFlakeHelper
{
    private static readonly object Lock = new();
    private static SnowFlakeHelper _instance;

    public static SnowFlakeHelper Initialize(long datacenterId = 1, long workerId = 1)
    {
        //双if 加锁
        if (_instance == null)
        {
            lock (Lock)
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
    private const long Twepoch = 1577836800000L;

    // 机器id所占的位数
    private const int WorkerIdBits = 5;

    // 数据标识id所占的位数
    private const int DatacenterIdBits = 5;

    // 支持的最大机器id，结果是31 (这个移位算法可以很快的计算出几位二进制数所能表示的最大十进制数)
    private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);

    // 支持的最大数据标识id，结果是31
    private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);

    // 序列在id中占的位数
    private const int SequenceBits = 12;

    // 数据标识id向左移17位(12+5)
    private const int DatacenterIdShift = SequenceBits + WorkerIdBits;

    // 机器ID向左移12位
    private const int WorkerIdShift = SequenceBits;


    // 时间截向左移22位(5+5+12)
    private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;

    // 生成序列的掩码，这里为4095 (0b111111111111=0xfff=4095)
    private const long sequenceMask = -1L ^ (-1L << SequenceBits);

    // 数据中心ID(0~31)
    public long DatacenterId { get; }

    // 工作机器ID(0~31)
    public long WorkerId { get; }

    // 毫秒内序列(0~4095)
    public long Sequence { get; private set; }

    // 上次生成ID的时间截
    public long LastTimestamp { get; private set; }


    /// <summary>
    /// 雪花ID
    /// </summary>
    /// <param name="datacenterId">数据中心ID</param>
    /// <param name="workerId">工作机器ID</param>
    private SnowFlakeHelper(long datacenterId, long workerId)
    {
        if (datacenterId is > MaxDatacenterId or < 0)
        {
            throw new Exception($"datacenter Id can't be greater than {MaxDatacenterId} or less than 0");
        }

        if (workerId is > MaxWorkerId or < 0)
        {
            throw new Exception($"worker Id can't be greater than {MaxWorkerId} or less than 0");
        }

        WorkerId = workerId;
        DatacenterId = datacenterId;
        Sequence = 0L;
        LastTimestamp = -1L;
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
            if (timestamp > LastTimestamp) //时间戳改变，毫秒内序列重置
            {
                Sequence = 0L;
            }
            else if (timestamp == LastTimestamp) //如果是同一时间生成的，则进行毫秒内序列
            {
                Sequence = (Sequence + 1) & sequenceMask;
                if (Sequence == 0) //毫秒内序列溢出
                {
                    timestamp = GetNextTimestamp(LastTimestamp); //阻塞到下一个毫秒,获得新的时间戳
                }
            }
            else //当前时间小于上一次ID生成的时间戳，证明系统时钟被回拨，此时需要做回拨处理
            {
                Sequence = (Sequence + 1) & sequenceMask;
                if (Sequence > 0)
                {
                    timestamp = LastTimestamp; //停留在最后一次时间戳上，等待系统时间追上后即完全度过了时钟回拨问题。
                }
                else //毫秒内序列溢出
                {
                    timestamp = LastTimestamp + 1; //直接进位到下一个毫秒
                }
                //throw new Exception(string.Format("Clock moved backwards.  Refusing to generate id for {0} milliseconds", lastTimestamp - timestamp));
            }

            LastTimestamp = timestamp; //上次生成ID的时间截

            //移位并通过或运算拼到一起组成64位的ID
            var id = ((timestamp - Twepoch) << TimestampLeftShift)
                     | (DatacenterId << DatacenterIdShift)
                     | (WorkerId << WorkerIdShift)
                     | Sequence;
            return id;
        }
    }

    /// <summary>
    /// 解析雪花ID
    /// </summary>
    /// <returns></returns>
    public static string AnalyzeId(long Id)
    {
        var sb = new StringBuilder();

        var timestamp = (Id >> TimestampLeftShift);
        var time = Jan1St1970.AddMilliseconds(timestamp + Twepoch);
        sb.Append(time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss:fff"));

        var datacenterId = (Id ^ (timestamp << TimestampLeftShift)) >> DatacenterIdShift;
        sb.Append("_" + datacenterId);

        var workerId = (Id ^ ((timestamp << TimestampLeftShift) | (datacenterId << DatacenterIdShift))) >> WorkerIdShift;
        sb.Append("_" + workerId);

        var sequence = Id & sequenceMask;
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
        return (long) (DateTime.UtcNow - Jan1St1970).TotalMilliseconds;
    }

    private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}