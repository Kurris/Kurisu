using Kurisu.EFSharding.Core.Internal;
using Kurisu.EFSharding.Sharding.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kurisu.EFSharding.EFCores;

public class ShardingLocalView<TEntity>:LocalView<TEntity>  where TEntity : class
{
    private readonly DbContext _dbContext;

    public ShardingLocalView(DbSet<TEntity> set) : base(set)
    {
        _dbContext = set.GetService<ICurrentDbContext>().Context;
    }

    public override IEnumerator<TEntity> GetEnumerator()
    {
        if (_dbContext is IShardingDbContext shardingDbContext)
        {
            var dataSourceDbContexts = shardingDbContext.GetShardingExecutor().GetCurrentDbContexts();
            var enumerators = dataSourceDbContexts.SelectMany(o => o.Value.GetCurrentContexts().Select(cd=>cd.Value.Set<TEntity>().Local.GetEnumerator()));
            return new MultiEnumerator<TEntity>(enumerators);
        }
        return base.GetEnumerator();
    }
}