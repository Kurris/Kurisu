using System.Collections.Generic;
using System.Linq;
using Kurisu.DataAccess.Sharding.Metadata;

namespace Kurisu.DataAccess.Sharding.VirtualRoute.TableRoute.Abstractions;

public abstract class AbstractVirtualTableRoute<TEntity> : IVirtualTableRoute<TEntity> where TEntity : class, new()
{
    public abstract void OnMetadataBuilder(MetadataBuilder<TEntity> builder);


    public EntityMetadata Metadata { get; }


    public List<TableRouteUnit> RouteWithPredicate(DatasourceRouteResult dataSourceRouteResult, IQueryable queryable, bool isQuery)
    {
        throw new System.NotImplementedException();
    }

    public TableRouteUnit RouteWithValue(DatasourceRouteResult dataSourceRouteResult, object shardingKey)
    {
        throw new System.NotImplementedException();
    }

    public abstract IEnumerable<string> GetTails();

    public bool EnableEntityQuery { get; }
}