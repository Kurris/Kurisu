using System.Data.Common;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.EFSharding.EFCores.EFCore6x.Tx;

public class ShardingRelationalTransaction : RelationalTransaction
{
    private readonly IShardingDbContext _shardingDbContext;
    private readonly IShardingDbContextExecutor _shardingDbContextExecutor;

    public ShardingRelationalTransaction(IShardingDbContext shardingDbContext, IRelationalConnection connection, DbTransaction transaction, Guid transactionId, IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned, ISqlGenerationHelper sqlGenerationHelper) : base(connection, transaction, transactionId, logger, transactionOwned, sqlGenerationHelper)
    {
        _shardingDbContext =
            shardingDbContext ?? throw new ShardingCoreInvalidOperationException($"should implement {nameof(IShardingDbContext)}");
        _shardingDbContextExecutor = shardingDbContext.GetShardingExecutor() ??
                                     throw new ShardingCoreInvalidOperationException(
                                         $"{shardingDbContext.GetType()} cant get {nameof(IShardingDbContextExecutor)} from {nameof(shardingDbContext.GetShardingExecutor)}");
    }

    public override void Commit()
    {
        base.Commit();
        _shardingDbContextExecutor.Commit();
        _shardingDbContextExecutor.NotifyShardingTransaction();
    }

    public override void Rollback()
    {
        base.Rollback();
        _shardingDbContextExecutor.Rollback();
        _shardingDbContextExecutor.NotifyShardingTransaction();
    }

    public override async Task RollbackAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        await base.RollbackAsync(cancellationToken);

        await _shardingDbContextExecutor.RollbackAsync(cancellationToken);
        _shardingDbContextExecutor.NotifyShardingTransaction();
    }

    public override async Task CommitAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        await base.CommitAsync(cancellationToken);

        await _shardingDbContextExecutor.CommitAsync(cancellationToken);
        _shardingDbContextExecutor.NotifyShardingTransaction();
    }

    public override void CreateSavepoint(string name)
    {
        base.CreateSavepoint(name);
        _shardingDbContextExecutor.CreateSavepoint(name);
    }

    public override async Task CreateSavepointAsync(string name, CancellationToken cancellationToken = new CancellationToken())
    {
        await base.CreateSavepointAsync(name, cancellationToken);
        await _shardingDbContextExecutor.CreateSavepointAsync(name, cancellationToken);
    }

    public override void RollbackToSavepoint(string name)
    {
        base.RollbackToSavepoint(name);
        _shardingDbContextExecutor.RollbackToSavepoint(name);
    }

    public override async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = new CancellationToken())
    {
        await base.RollbackToSavepointAsync(name, cancellationToken);
        await _shardingDbContextExecutor.RollbackToSavepointAsync(name, cancellationToken);
    }

    public override void ReleaseSavepoint(string name)
    {
        base.ReleaseSavepoint(name);
        _shardingDbContextExecutor.ReleaseSavepoint(name);
    }

    public override async Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = new CancellationToken())
    {
        await base.ReleaseSavepointAsync(name, cancellationToken);
        await _shardingDbContextExecutor.ReleaseSavepointAsync(name, cancellationToken);
    }
}