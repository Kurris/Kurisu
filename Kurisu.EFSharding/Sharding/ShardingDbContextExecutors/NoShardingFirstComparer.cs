using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails;

namespace Kurisu.EFSharding.Sharding.ShardingDbContextExecutors;

/// <summary>
/// 未分表后缀dbcontext排序器
/// 用来针对data source db context多个迭代的时候优先级,默认未分表的排在第一格
/// </summary>
public class NoShardingFirstComparer:IComparer<string>
{
    private readonly string _defaultTail;

    /// <summary>
    /// 未分表后缀dbcontext排序器
    /// </summary>
    public NoShardingFirstComparer()
    {
        _defaultTail = new SingleQueryRouteTail(string.Empty).GetRouteTailIdentity();
    }

    /// <summary>
    /// 当 x或者y为默认为分表后缀，那么就将对应的排序指定顺序x返回-1，y返回1不进行比较
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public int Compare(string? x, string? y)
    {
        if (!Object.Equals(x, y))
        {
            if (_defaultTail.Equals(x))
                return -1;
            if (_defaultTail.Equals(y))
                return 1;
        }
        return Comparer<string>.Default.Compare(x, y);
    }
}