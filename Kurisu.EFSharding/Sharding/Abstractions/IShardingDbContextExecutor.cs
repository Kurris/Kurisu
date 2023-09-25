using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IShardingDbContextExecutor : IShardingTransaction, IReadWriteSwitch, ICurrentDbContextDiscover, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// has multi db context
    /// </summary>
    bool IsMultiDbContext { get; }


    /// <summary>
    /// create sharding db context options
    /// </summary>
    /// <param name="strategy">如果当前查询需要多链接的情况下那么将使用<code>IndependentConnectionQuery</code>否则使用<code>ShareConnection</code></param>
    /// <param name="datasourceName">data source name</param>
    /// <param name="routeTail"></param>
    /// <returns></returns>
    DbContext CreateDbContext(CreateDbContextStrategyEnum strategy, string datasourceName, IRouteTail routeTail);

    /// <summary>
    /// create db context by entity
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    DbContext CreateGenericDbContext<TEntity>(TEntity entity) where TEntity : class;

    IVirtualDatasource GetVirtualDatasource();

    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = new CancellationToken());

    int SaveChanges(bool acceptAllChangesOnSuccess);

    DbContext GetShellDbContext();
}