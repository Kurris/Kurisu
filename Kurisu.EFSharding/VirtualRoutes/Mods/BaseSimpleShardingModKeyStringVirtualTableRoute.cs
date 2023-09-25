using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.Abstractions;
using Kurisu.EFSharding.Helpers;

namespace Kurisu.EFSharding.VirtualRoutes.Mods;

public abstract class BaseSimpleShardingModKeyStringVirtualTableRoute<TEntity> : AbstractShardingOperatorVirtualTableRoute<TEntity, string>
    where TEntity : class, new()
{
    private readonly int _mod;
    private readonly int _tailLength;
    protected virtual char Padding => '0';

    protected BaseSimpleShardingModKeyStringVirtualTableRoute(IShardingProvider shardingProvider, int tailLength, int mod) : base(shardingProvider)
    {
        _tailLength = tailLength;
        _mod = mod;
    }


    public override string ToTail(object shardingKey)
    {
        var shardingKeyStr = shardingKey.ToString();
        return Math.Abs(ShardingCoreHelper.GetStringHashCode(shardingKeyStr) % _mod).ToString().PadLeft(_tailLength, Padding);
    }

    /// <summary>
    /// 获取对应类型在数据库中的所有后缀
    /// </summary>
    /// <returns></returns>
    public override List<string> GetTails()
    {
        return Enumerable.Range(0, _mod).Select(o => o.ToString().PadLeft(_tailLength, Padding)).ToList();
    }

    /// <summary>
    /// 路由表达式如何路由到正确的表
    /// </summary>
    /// <param name="shardingKey"></param>
    /// <param name="shardingOperator"></param>
    /// <returns></returns>
    protected override Func<string, bool> GetRouteToFilter(string shardingKey, ShardingOperatorEnum shardingOperator)
    {
        var t = ToTail(shardingKey);
        return shardingOperator switch
        {
            ShardingOperatorEnum.Equal => tail => tail == t,
            _ => _ => true
        };
    }
}