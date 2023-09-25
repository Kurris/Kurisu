using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;

namespace Kurisu.EFSharding.Core.QueryTrackers;

public class QueryTracker : IQueryTracker
{
    public object Track(object entity, IShardingDbContext shardingDbContext)
    {
        var genericDbContext = shardingDbContext.GetShardingExecutor().CreateGenericDbContext(entity);
        var attachedEntity = genericDbContext.GetAttachedEntity(entity);
        if (attachedEntity == null)
            genericDbContext.Attach(entity);
        else
        {
            return attachedEntity;
        }

        return null;
    }
}