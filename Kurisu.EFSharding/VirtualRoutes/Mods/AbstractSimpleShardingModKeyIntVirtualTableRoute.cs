using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.Abstractions;

namespace Kurisu.EFSharding.VirtualRoutes.Mods;

/// <summary>
/// 分表字段为int的取模分表
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public abstract class AbstractSimpleShardingModKeyIntVirtualTableRoute<TEntity> : AbstractShardingOperatorVirtualTableRoute<TEntity, int> where TEntity : class, new()
{
    private readonly int _mod;
    private readonly int _tailLength;

    /// <summary>
    /// 当取模后不足tailLength左补什么参数
    /// </summary>
    protected virtual char PaddingChar => '0';


    protected AbstractSimpleShardingModKeyIntVirtualTableRoute(IShardingProvider shardingProvider, int tailLength, int mod) : base(shardingProvider)
    {
        _tailLength = tailLength;
        _mod = mod;
    }

    public override string ToTail(object shardingKey)
    {
        var shardingKeyInt = Convert.ToInt32(shardingKey);
        return Math.Abs(shardingKeyInt % _mod).ToString().PadLeft(_tailLength, PaddingChar);
    }

    public override List<string> GetTails()
    {
        return Enumerable.Range(0, _mod).Select(o => o.ToString().PadLeft(_tailLength, PaddingChar)).ToList();
    }

    protected override Func<string, bool> GetRouteToFilter(int shardingKey, ShardingOperatorEnum shardingOperator)
    {
        var t = ToTail(shardingKey);
        return shardingOperator switch
        {
            ShardingOperatorEnum.Equal => tail => tail == t,
            _ => _ => true
        };
    }
}