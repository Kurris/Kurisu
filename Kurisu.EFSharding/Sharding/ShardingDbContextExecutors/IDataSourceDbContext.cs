using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.ShardingDbContextExecutors;

/// <summary>
/// 同数据源下的dbcontext管理者
/// </summary>
public interface IDatasourceDbContext : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// is default data source connection string
    /// </summary>
    bool IsDefault { get; }

    int DbContextCount { get; }
    DbContext CreateDbContext(IRouteTail routeTail);

    /// <summary>
    /// 通知事务自动管理是否要清理还是开启还是加入事务
    /// </summary>
    void NotifyTransaction();

    int SaveChanges(bool acceptAllChangesOnSuccess);

    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken());

    IDictionary<string, DbContext> GetCurrentContexts();

    void Rollback();
    void Commit();

    Task RollbackAsync(CancellationToken cancellationToken = new CancellationToken());
    Task CommitAsync(CancellationToken cancellationToken = new CancellationToken());

    void CreateSavepoint(string name);
    Task CreateSavepointAsync(string name, CancellationToken cancellationToken = new CancellationToken());
    void RollbackToSavepoint(string name);
    Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default(CancellationToken));
    void ReleaseSavepoint(string name);
    Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default(CancellationToken));
}