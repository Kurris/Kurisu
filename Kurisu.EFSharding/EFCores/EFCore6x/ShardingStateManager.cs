using System.Diagnostics.CodeAnalysis;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.EFSharding.EFCores.EFCore6x;

[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
internal class ShardingStateManager : StateManager
{
    private readonly DbContext _currentDbContext;
    private readonly IShardingDbContext _currentShardingDbContext;

    public ShardingStateManager(StateManagerDependencies dependencies) : base(dependencies)
    {
        _currentDbContext = dependencies.CurrentContext.Context;
        _currentShardingDbContext = (IShardingDbContext) _currentDbContext;
    }

    public override InternalEntityEntry GetOrCreateEntry(object entity)
    {
        var genericDbContext = _currentShardingDbContext.GetShardingExecutor().CreateGenericDbContext(entity);
        var dbContextDependencies = genericDbContext.GetService<IDbContextDependencies>();
        var stateManager = dbContextDependencies.StateManager;
        return stateManager.GetOrCreateEntry(entity);
    }

    public override InternalEntityEntry GetOrCreateEntry(object entity, IEntityType entityType)
    {
        var genericDbContext = _currentShardingDbContext.GetShardingExecutor().CreateGenericDbContext(entity);
        var findEntityType = genericDbContext.Model.FindEntityType(entity.GetType());
        var dbContextDependencies = genericDbContext.GetService<IDbContextDependencies>();
        var stateManager = dbContextDependencies.StateManager;
        return stateManager.GetOrCreateEntry(entity, findEntityType);
    }

    public override InternalEntityEntry StartTrackingFromQuery(IEntityType baseEntityType, object entity, in ValueBuffer valueBuffer)
    {
        throw new ShardingCoreNotImplementedException();
    }

    public override InternalEntityEntry TryGetEntry(object entity, bool throwOnNonUniqueness = true)
    {
        throw new ShardingCoreNotImplementedException();
    }

    public override InternalEntityEntry TryGetEntry(object entity, IEntityType entityType, bool throwOnTypeMismatch = true)
    {
        throw new ShardingCoreNotImplementedException();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        //ApplyShardingConcepts();
        int i;
        //如果是内部开的事务就内部自己消化
        if (_currentDbContext.Database.AutoTransactionsEnabled && _currentDbContext.Database.CurrentTransaction == null && _currentShardingDbContext.GetShardingExecutor().IsMultiDbContext)
        {
            using var tran = _currentDbContext.Database.BeginTransaction();
            i = _currentShardingDbContext.GetShardingExecutor().SaveChanges(acceptAllChangesOnSuccess);
            tran.Commit();
        }
        else
        {
            i = _currentShardingDbContext.GetShardingExecutor().SaveChanges(acceptAllChangesOnSuccess);
        }

        return i;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        //ApplyShardingConcepts();
        int i;
        //如果是内部开的事务就内部自己消化
        if (_currentDbContext.Database.AutoTransactionsEnabled && _currentDbContext.Database.CurrentTransaction == null && _currentShardingDbContext.GetShardingExecutor().IsMultiDbContext)
        {
            await using var tran = await _currentDbContext.Database.BeginTransactionAsync(cancellationToken);
            i = await _currentShardingDbContext.GetShardingExecutor().SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            await tran.CommitAsync(cancellationToken);
        }
        else
        {
            i = await _currentShardingDbContext.GetShardingExecutor().SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }


        return i;
    }
}