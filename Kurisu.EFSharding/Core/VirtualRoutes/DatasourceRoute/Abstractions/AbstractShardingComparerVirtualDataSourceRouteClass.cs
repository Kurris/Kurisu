using Kurisu.EFSharding.Core.DependencyInjection;

namespace Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute.Abstractions;

public abstract class AbstractShardingComparerVirtualDatasourceRouteClass<TEntity, TKey> : AbstractShardingOperatorVirtualDataSourceRoute<TEntity, TKey> where TEntity : class, new()
{
    protected AbstractShardingComparerVirtualDatasourceRouteClass(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }

    protected abstract IComparer<string> GetComparer();

    protected override Func<string, bool> GetRouteToFilter(TKey shardingKey, ShardingOperatorEnum shardingOperator)
    {
        var dataSourceName = ShardingKeyToDataSourceName(shardingKey);
        var comparer = GetComparer();
        switch (shardingOperator)
        {
            case ShardingOperatorEnum.GreaterThan:
            case ShardingOperatorEnum.GreaterThanOrEqual:
                return dataSource => comparer.Compare(dataSource, dataSourceName) >= 0;
            case ShardingOperatorEnum.LessThan:
            case ShardingOperatorEnum.LessThanOrEqual:
                return dataSource => comparer.Compare(dataSource, dataSourceName) <= 0;
            case ShardingOperatorEnum.Equal: return dataSource => comparer.Compare(dataSource, dataSourceName) == 0;
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