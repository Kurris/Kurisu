using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Helpers;
using Kurisu.EFSharding.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.EFSharding.EFCores.EFCore6x;

public class ShardingMigrator : Migrator
{
    private readonly IShardingRuntimeContext _shardingRuntimeContext;

    public ShardingMigrator(IShardingRuntimeContext shardingRuntimeContext, IMigrationsAssembly migrationsAssembly, IHistoryRepository historyRepository, IDatabaseCreator databaseCreator, IMigrationsSqlGenerator migrationsSqlGenerator, IRawSqlCommandBuilder rawSqlCommandBuilder, IMigrationCommandExecutor migrationCommandExecutor, IRelationalConnection connection, ISqlGenerationHelper sqlGenerationHelper, ICurrentDbContext currentContext, IModelRuntimeInitializer modelRuntimeInitializer, IDiagnosticsLogger<DbLoggerCategory.Migrations> logger, IRelationalCommandDiagnosticsLogger commandLogger, IDatabaseProvider databaseProvider) : base(migrationsAssembly, historyRepository, databaseCreator, migrationsSqlGenerator, rawSqlCommandBuilder, migrationCommandExecutor, connection, sqlGenerationHelper, currentContext, modelRuntimeInitializer, logger, commandLogger, databaseProvider)
    {
        _shardingRuntimeContext = shardingRuntimeContext;
    }

    public override void Migrate(string targetMigration = null)
    {
        MigrateAsync(targetMigration).WaitAndUnwrapException(false);
        // base.Migrate(targetMigration);
    }

    public override async Task MigrateAsync(string targetMigration = null, CancellationToken cancellationToken = new CancellationToken())
    {
        var virtualdatasource = _shardingRuntimeContext.GetVirtualDatasource();
        var alldatasourceNames = virtualdatasource.GetAllDatasourceNames();
        await DynamicShardingHelper.DynamicMigrateWithDatasourceAsync(_shardingRuntimeContext, alldatasourceNames, null, targetMigration, cancellationToken).ConfigureAwait(false);
    }

    public override string GenerateScript(string fromMigration = null, string toMigration = null,
        MigrationsSqlGenerationOptions options = MigrationsSqlGenerationOptions.Default)
    {
        return new ScriptMigrationGenerator(_shardingRuntimeContext, fromMigration, toMigration, options).GenerateScript();
    }
}