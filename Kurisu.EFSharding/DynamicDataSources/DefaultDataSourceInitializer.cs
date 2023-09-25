using Kurisu.EFSharding.Core.DbContextCreator;
using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.Metadata.Model;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Dynamicdatasources;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.TableCreator;
using Kurisu.EFSharding.TableExists;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.Extensions.Logging;

namespace Kurisu.EFSharding.DynamicDataSources;

internal class DatasourceInitializer : IDatasourceInitializer
{
    private readonly ILogger<DatasourceInitializer> _logger;

    private readonly IShardingProvider _shardingProvider;
    private readonly IDbContextCreator _dbContextCreator;
    private readonly IVirtualDatasource _virtualdatasource;
    private readonly IRouteTailFactory _routeTailFactory;
    private readonly IDatasourceRouteManager _datasourceRouteManager;
    private readonly ITableRouteManager _tableRouteManager;
    private readonly IMetadataManager _entityMetadataManager;
    private readonly IShardingTableCreator _tableCreator;
    private readonly ITableEnsureManager _tableEnsureManager;

    public DatasourceInitializer(
        IShardingProvider shardingProvider,
        IDbContextCreator dbContextCreator,
        IVirtualDatasource virtualDatasource,
        IRouteTailFactory routeTailFactory,
        IDatasourceRouteManager datasourceRouteManager,
        ITableRouteManager tableRouteManager,
        IMetadataManager entityMetadataManager,
        IShardingTableCreator shardingTableCreator,
        ITableEnsureManager tableEnsureManager,
        ILogger<DatasourceInitializer> logger)
    {
        _shardingProvider = shardingProvider;
        _dbContextCreator = dbContextCreator;
        _virtualdatasource = virtualDatasource;
        _routeTailFactory = routeTailFactory;
        _datasourceRouteManager = datasourceRouteManager;
        _tableRouteManager = tableRouteManager;
        _entityMetadataManager = entityMetadataManager;
        _tableCreator = shardingTableCreator;
        _tableEnsureManager = tableEnsureManager;
        _logger = logger;
    }

    public async Task InitConfigureAsync(string datasourceName, bool createDatabase, bool createTable)
    {
        using var shardingScope = _shardingProvider.CreateScope();
        await using var shellDbContext = _dbContextCreator.GetShellDbContext(shardingScope.ServiceProvider);
        var isDefault = _virtualdatasource.IsDefault(datasourceName);

        if (createDatabase)
        {
            EnsureCreated(isDefault, shellDbContext as IShardingDbContext, datasourceName);
        }

        if (createTable)
        {
            var existTables = await _tableEnsureManager.GetExistTablesAsync((IShardingDbContext) shellDbContext, datasourceName);
            var allShardingEntities = _entityMetadataManager.GetAllShardingEntities();

            foreach (var entityType in allShardingEntities)
            {
                var metadataList = _entityMetadataManager.TryGet(entityType);
                if (metadataList.Any(x => x.IsDatasourceMetadata))
                {
                    var virtualDatasourceRoute = _datasourceRouteManager.GetRoute(entityType);
                    //如果是分库对象就要判断是否含有当前数据源
                    if (virtualDatasourceRoute.GetAllDatasourceNames().Contains(datasourceName))
                    {
                        await CreateDataTableAsync(datasourceName, metadataList, existTables);
                    }
                }
                //不是分库对象
                else
                {
                    if (_virtualdatasource.IsDefault(datasourceName))
                    {
                        await CreateDataTableAsync(datasourceName, metadataList, existTables);
                    }
                }
            }
        }
    }

    private void EnsureCreated(bool isDefault, IShardingDbContext context, string datasourceName)
    {
        if (context == null)
        {
            throw new ShardingCoreInvalidOperationException(
                $"{nameof(IDbContextCreator)}.{nameof(IDbContextCreator.GetShellDbContext)} db context type not impl {nameof(IShardingDbContext)}");
        }

        using var dbContext = context.GetIndependentWriteDbContext(datasourceName, _routeTailFactory.Create(string.Empty, false));

        _ = isDefault ? dbContext.RemoveShardingTables() : dbContext.RemoveWithoutShardingDatasourceOnly();
        dbContext.Database.EnsureCreated();
    }

    private async Task CreateDataTableAsync(string datasourceName, IReadOnlyCollection<BaseShardingMetadata> metadataList, ICollection<string> existTables)
    {
        if (metadataList.All(x => !x.IsTableMetadata))
        {
            var physicTableName = $"{metadataList.First().TableName}";
            try
            {
                //添加物理表
                if (!existTables.Contains(physicTableName))
                    await _tableCreator.CreateTableAsync(datasourceName, metadataList.First().ClrType, string.Empty);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Table {TableName} maybe created.", physicTableName);
            }
        }
        else
        {
            var tableRoute = _tableRouteManager.GetRoute(metadataList.First().ClrType);
            foreach (var tail in tableRoute.GetTails())
            {
                //todo tableSeparator
                var physicTableName = $"{metadataList.First().TableName}_{tail}";
                try
                {
                    //添加物理表
                    if (!existTables.Contains(physicTableName))
                        await _tableCreator.CreateTableAsync(datasourceName, metadataList.First().ClrType, tail);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"table :{physicTableName} maybe created.");
                }
            }
        }
    }
}