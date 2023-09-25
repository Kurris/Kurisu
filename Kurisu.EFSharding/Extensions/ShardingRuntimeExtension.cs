using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Exceptions;

namespace Kurisu.EFSharding.Extensions;

public static class ShardingRuntimeExtension
{
    /// <summary>
    /// 自动尝试补偿表
    /// </summary>
    /// <param name="shardingRuntimeContext"></param>
    /// <param name="parallelCount"></param>
    public static async Task UseAutoTryCompensateTable(this IShardingRuntimeContext shardingRuntimeContext, int parallelCount = 4)
    {
        var virtualDatasource = shardingRuntimeContext.GetVirtualDatasource();
        var datasourceInitializer = shardingRuntimeContext.GetDatasourceInitializer();

        if (parallelCount <= 0)
        {
            throw new ShardingCoreInvalidOperationException($"compensate table parallel count must > 0");
        }

        var allDatasourceNames = virtualDatasource.GetAllDatasourceNames();
        var partitionMigrationUnits = allDatasourceNames.Chunk(parallelCount);
        foreach (var datasources in partitionMigrationUnits)
        {
            var migrateUnits = datasources.Select(o => o).ToList();

            foreach (var datasource in migrateUnits)
            {
                await datasourceInitializer.InitConfigureAsync(datasource, true, true);
            }
        }
    }
}