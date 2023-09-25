using Kurisu.EFSharding.Core.DependencyInjection;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.Abstractions;

public abstract class AbstractShardingComparerVirtualTableRoute<TEntity, TKey> : AbstractShardingOperatorVirtualTableRoute<TEntity, TKey>
    where TEntity : class, new()
{
    protected AbstractShardingComparerVirtualTableRoute(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }

    protected abstract IComparer<string> GetComparer();

    protected override Func<string, bool> GetRouteToFilter(TKey shardingKey, ShardingOperatorEnum shardingOperator)
    {
        var t = ToTail(shardingKey);
        var comparer = GetComparer();
        switch (shardingOperator)
        {
            case ShardingOperatorEnum.GreaterThan:
            case ShardingOperatorEnum.GreaterThanOrEqual:
                return tail => comparer.Compare(tail, t) >= 0;
            case ShardingOperatorEnum.LessThan:
            case ShardingOperatorEnum.LessThanOrEqual:
                return tail => comparer.Compare(tail, t) <= 0;
            case ShardingOperatorEnum.Equal: return tail => comparer.Compare(tail, t) == 0;
            case ShardingOperatorEnum.UnKnown:
            case ShardingOperatorEnum.NotEqual:
            case ShardingOperatorEnum.AllLike:
            case ShardingOperatorEnum.StartLike:
            case ShardingOperatorEnum.EndLike:
            default:
            {
                return _ => true;
            }
        }
    }
}