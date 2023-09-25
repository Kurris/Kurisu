using System.Data.Common;
using Kurisu.EFSharding.Sharding.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.EFSharding.EFCores.EFCore6x.Tx;


public class ShardingRelationalTransactionFactory : RelationalTransactionFactory
{
    private readonly RelationalTransactionFactoryDependencies _dependencies;

    public ShardingRelationalTransactionFactory(RelationalTransactionFactoryDependencies dependencies) : base(dependencies)
    {
        _dependencies = dependencies;
    }

    public override RelationalTransaction Create(IRelationalConnection connection, DbTransaction transaction, Guid transactionId,
        IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger, bool transactionOwned)
    {
        var shardingDbContext = connection.Context as IShardingDbContext;
        return new ShardingRelationalTransaction(shardingDbContext, connection, transaction, transactionId, logger, transactionOwned, this.Dependencies.SqlGenerationHelper);
    }
}