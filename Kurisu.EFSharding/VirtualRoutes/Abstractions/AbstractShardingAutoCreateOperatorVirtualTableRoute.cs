using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.Metadata.Model;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.Abstractions;

namespace Kurisu.EFSharding.VirtualRoutes.Abstractions;

/// <summary>
/// 分片字段追加
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public abstract class AbstractShardingAutoCreateOperatorVirtualTableRoute<TEntity, TKey> : AbstractShardingOperatorVirtualTableRoute<TEntity, TKey>
    where TEntity : class, new()
{
    protected AbstractShardingAutoCreateOperatorVirtualTableRoute(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }


    protected abstract List<string> CalcTailsOnStart();


    private List<string> _tails;

    public override void Initialize(BaseShardingMetadata entityMetadata)
    {
        base.Initialize(entityMetadata);
        _tails = CalcTailsOnStart();
    }

    public override List<string> GetTails()
    {
        return _tails;
    }

    /// <summary>
    /// 当前时间转成
    /// </summary>
    /// <param name="now"></param>
    /// <returns></returns>
    protected abstract string ConvertNowToTail(DateTime now);
}