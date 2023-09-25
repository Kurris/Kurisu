using System.Text;
using Kurisu.EFSharding.Core.DbContextCreator;
using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Helpers;
using Kurisu.EFSharding.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kurisu.EFSharding.EFCores;

public abstract class AbstractScriptMigrationGenerator
{
    private readonly IShardingRuntimeContext _shardingRuntimeContext;

    public AbstractScriptMigrationGenerator(IShardingRuntimeContext shardingRuntimeContext)
    {
        _shardingRuntimeContext = shardingRuntimeContext;
    }

    public string GenerateScript()
    {
        var virtualDatasource = _shardingRuntimeContext.GetVirtualDatasource();
        var allDatasourceNames = virtualDatasource.GetAllDatasourceNames();
        var dbContextCreator = _shardingRuntimeContext.GetDbContextCreator();
        var shardingProvider = _shardingRuntimeContext.GetShardingProvider();
        var shardingConfigOptions = _shardingRuntimeContext.GetShardingConfigOptions();
        var defaultDatasourceName = virtualDatasource.DefaultDatasourceName;

        using (var scope = shardingProvider.CreateScope())
        {
            using (var shellDbContext = dbContextCreator.GetShellDbContext(scope.ServiceProvider))
            {
                var parallelCount = shardingConfigOptions.MigrationParallelCount;
                if (parallelCount <= 0)
                {
                    throw new ShardingCoreInvalidOperationException($"migration parallel count must >0");
                }

                //默认数据源需要最后执行 否则可能会导致异常的情况下GetPendingMigrations为空
                var partitionMigrationUnits = allDatasourceNames.Where(o => o != defaultDatasourceName)
                    .Chunk(parallelCount);
                var scriptStringBuilder = new StringBuilder();
                foreach (var migrationUnits in partitionMigrationUnits)
                {
                    var migrateUnits = migrationUnits.Select(o => new MigrateUnit(shellDbContext, o)).ToList();
                    var scriptSql = ExecuteMigrateUnits(_shardingRuntimeContext, migrateUnits);
                    scriptStringBuilder.AppendLine(scriptSql);
                }

                //包含默认默认的单独最后一次处理
                if (allDatasourceNames.Contains(defaultDatasourceName))
                {
                    var scriptSql = ExecuteMigrateUnits(_shardingRuntimeContext,
                        new List<MigrateUnit>() { new MigrateUnit(shellDbContext, defaultDatasourceName) });
                    scriptStringBuilder.AppendLine(scriptSql);
                }

                return scriptStringBuilder.ToString();
            }
        }
    }

    private string ExecuteMigrateUnits(IShardingRuntimeContext shardingRuntimeContext,
        List<MigrateUnit> migrateUnits)
    {
        var shardingMigrationManager = shardingRuntimeContext.GetShardingMigrationManager();
        var dbContextCreator = shardingRuntimeContext.GetDbContextCreator();
        var routeTailFactory = shardingRuntimeContext.GetRouteTailFactory();
        var migrateTasks = migrateUnits.Select(migrateUnit =>
        {
            return Task.Run(() =>
            {
                using (shardingMigrationManager.CreateScope())
                {
                    shardingMigrationManager.Current.CurrentDatasourceName = migrateUnit.DatasourceName;

                    var dbContextOptions = DynamicShardingHelper.CreateShellDbContextOptions(shardingRuntimeContext,
                        migrateUnit.DatasourceName);

                    using (var dbContext = dbContextCreator.CreateDbContext(migrateUnit.ShellDbContext,
                               new ShardingDbContextOptions(dbContextOptions,
                                   routeTailFactory.Create(string.Empty, false))))
                    {
                        var migrator = dbContext.GetService<IMigrator>();
                        return $"-- datasource:{migrateUnit.DatasourceName}" + Environment.NewLine +
                               GenerateScriptSql(migrator) +
                               Environment.NewLine;
                    }
                }
            });
        }).ToArray();
        var scripts = TaskHelper.WhenAllFastFail(migrateTasks).WaitAndUnwrapException();
        return string.Join(Environment.NewLine, scripts);
    }

    protected abstract string GenerateScriptSql(IMigrator migrator);
}