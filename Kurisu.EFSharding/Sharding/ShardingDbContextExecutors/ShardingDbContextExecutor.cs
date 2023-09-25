using System.Collections.Concurrent;
using Kurisu.EFSharding.Core.DbContextCreator;
using Kurisu.EFSharding.Core.Metadata.Manager;
using Kurisu.EFSharding.Core.RuntimeContexts;
using Kurisu.EFSharding.Core.TrackerManagers;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Core.VirtualRoutes.Abstractions;
using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;
using Kurisu.EFSharding.Extensions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kurisu.EFSharding.Sharding.ShardingDbContextExecutors;

/// <summary>
/// DbContext执行者
/// </summary>
public class ShardingDbContextExecutor : IShardingDbContextExecutor
{
    private readonly ILogger<ShardingDbContextExecutor> _logger;
    private readonly DbContext _shardingDbContext;

    private readonly ConcurrentDictionary<string, IDatasourceDbContext> _dbContextCaches = new();

    private readonly IShardingRuntimeContext _shardingRuntimeContext;
    private readonly IVirtualDatasource _virtualDatasource;
    private readonly IDatasourceRouteManager _dataSourceRouteManager;
    private readonly ITableRouteManager _tableRouteManager;
    private readonly IDbContextCreator _dbContextCreator;
    private readonly IRouteTailFactory _routeTailFactory;
    private readonly ITrackerManager _trackerManager;
    private readonly ActualConnectionStringManager _actualConnectionStringManager;
    private readonly IMetadataManager _entityMetadataManager;

    public int ReadWriteSeparationPriority
    {
        get => _actualConnectionStringManager.ReadWriteSeparationPriority;
        set => _actualConnectionStringManager.ReadWriteSeparationPriority = value;
    }

    [Obsolete("use ReadWriteSeparationBehavior")]
    public bool ReadWriteSeparation
    {
        get => _actualConnectionStringManager.ReadWriteSeparation == ReadWriteDefaultEnableBehavior.DefaultEnable;
        set => _actualConnectionStringManager.ReadWriteSeparation = value ? ReadWriteDefaultEnableBehavior.DefaultEnable : ReadWriteDefaultEnableBehavior.DefaultDisable;
    }

    public ReadWriteDefaultEnableBehavior ReadWriteSeparationBehavior
    {
        get => _actualConnectionStringManager.ReadWriteSeparation;
        set => _actualConnectionStringManager.ReadWriteSeparation = value;
    }


    public ShardingDbContextExecutor(DbContext shardingDbContext)
    {
        _shardingDbContext = shardingDbContext;

        _shardingRuntimeContext = shardingDbContext.GetShardingRuntimeContext();
        _virtualDatasource = _shardingRuntimeContext.GetVirtualDatasource();
        _dataSourceRouteManager = _shardingRuntimeContext.GetDatasourceRouteManager();
        _tableRouteManager = _shardingRuntimeContext.GetTableRouteManager();
        _dbContextCreator = _shardingRuntimeContext.GetDbContextCreator();
        _entityMetadataManager = _shardingRuntimeContext.GetMetadataManager();
        _routeTailFactory = _shardingRuntimeContext.GetRouteTailFactory();
        _trackerManager = _shardingRuntimeContext.GetTrackerManager();
        var shardingReadWriteManager = _shardingRuntimeContext.GetShardingReadWriteManager();
        var shardingProvider = _shardingRuntimeContext.GetShardingProvider();
        var loggerFactory = shardingProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<ShardingDbContextExecutor>();
        _actualConnectionStringManager =
            new ActualConnectionStringManager(shardingReadWriteManager, _virtualDatasource, _shardingDbContext);
    }


    private IDatasourceDbContext GetDataSourceDbContext(string datasourceName)
    {
        return _dbContextCaches.GetOrAdd(datasourceName, x =>
            new DataSourceDbContext(x, _virtualDatasource.IsDefault(x), _shardingDbContext, _dbContextCreator, _actualConnectionStringManager)
        );
    }

    /// <summary>
    /// has more db context
    /// </summary>
    public bool IsMultiDbContext => _dbContextCaches.Count > 1 || _dbContextCaches.Sum(o => o.Value.DbContextCount) > 1;

    public DbContext CreateDbContext(CreateDbContextStrategyEnum strategy, string datasourceName, IRouteTail routeTail)
    {
        if (CreateDbContextStrategyEnum.ShareConnection == strategy)
        {
            var dataSourceDbContext = GetDataSourceDbContext(datasourceName);
            return dataSourceDbContext.CreateDbContext(routeTail);
        }

        var parallelDbContextOptions = CreateParallelDbContextOptions(datasourceName, strategy);
        var dbContext = _dbContextCreator.CreateDbContext(_shardingDbContext, parallelDbContextOptions, routeTail);
        dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

        return dbContext;
    }

    private DbContextOptions CreateParallelDbContextOptions(string dataSourceName,
        CreateDbContextStrategyEnum strategy)
    {
        var dbContextOptionBuilder = _shardingRuntimeContext.GetDbContextOptionBuilderCreator()
            .CreateDbContextOptionBuilder();
        var connectionString = _actualConnectionStringManager.GetConnectionString(dataSourceName,
            CreateDbContextStrategyEnum.IndependentConnectionWrite == strategy);
        _virtualDatasource.UseDbContextOptionsBuilder(connectionString, dbContextOptionBuilder)
            .UseShardingOptions(_shardingRuntimeContext);
        return dbContextOptionBuilder.Options;
    }


    public DbContext CreateGenericDbContext<TEntity>(TEntity entity) where TEntity : class
    {
        var realEntityType = _trackerManager.TranslateEntityType(entity.GetType());
        var dataSourceName = GetDataSourceName(entity, realEntityType);
        var tail = GetTableTail(dataSourceName, entity, realEntityType);

        var dbContext = CreateDbContext(CreateDbContextStrategyEnum.ShareConnection, dataSourceName,
            _routeTailFactory.Create(tail));

        return dbContext;
    }

    public IVirtualDatasource GetVirtualDatasource()
    {
        return _virtualDatasource;
    }

    private string GetDataSourceName<TEntity>(TEntity entity, Type realEntityType) where TEntity : class
    {
        return _dataSourceRouteManager.GetDatasourceName(entity, realEntityType);
    }

    private string GetTableTail<TEntity>(string dataSourceName, TEntity entity, Type realEntityType) where TEntity : class
    {
        if (!_entityMetadataManager.IsShardingTable(realEntityType))
            return string.Empty;
        return _tableRouteManager.GetTableTail(dataSourceName, entity, realEntityType);
    }


    #region transaction

    public async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = new CancellationToken())
    {
        int i = 0;
        foreach (var dbContextCache in _dbContextCaches)
        {
            i += await dbContextCache.Value.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        AutoUseWriteConnectionString();
        return i;
    }

    public int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var i = 0;
        foreach (var dbContextCache in _dbContextCaches)
        {
            i += dbContextCache.Value.SaveChanges(acceptAllChangesOnSuccess);
        }

        AutoUseWriteConnectionString();
        return i;
    }

    public DbContext GetShellDbContext()
    {
        return _shardingDbContext;
    }

    public void NotifyShardingTransaction()
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            dbContextCache.Value.NotifyTransaction();
        }
    }

    public void Rollback()
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            dbContextCache.Value.Rollback();
        }

        AutoUseWriteConnectionString();
    }

    public void Commit()
    {
        var i = 0;
        foreach (var dbContextCache in _dbContextCaches)
        {
            try
            {
                dbContextCache.Value.Commit();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{nameof(Commit)} error.");
                if (i == 0)
                    throw;
            }

            i++;
        }

        AutoUseWriteConnectionString();
    }

    public IDictionary<string, IDatasourceDbContext> GetCurrentDbContexts()
    {
        return _dbContextCaches;
    }

    #endregion


    public void Dispose()
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            dbContextCache.Value.Dispose();
        }

        _dbContextCaches.Clear();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            await dbContextCache.Value.RollbackAsync(cancellationToken);
        }

        AutoUseWriteConnectionString();
    }

    public async Task CommitAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var i = 0;
        foreach (var dbContextCache in _dbContextCaches)
        {
            try
            {
                await dbContextCache.Value.CommitAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{nameof(CommitAsync)} error.");
                if (i == 0)
                    throw;
            }

            i++;
        }

        AutoUseWriteConnectionString();
    }

    public void CreateSavepoint(string name)
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            dbContextCache.Value.CreateSavepoint(name);
        }
    }

    public async Task CreateSavepointAsync(string name,
        CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            await dbContextCache.Value.CreateSavepointAsync(name, cancellationToken);
        }
    }

    public void RollbackToSavepoint(string name)
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            dbContextCache.Value.RollbackToSavepoint(name);
        }
    }

    public async Task RollbackToSavepointAsync(string name,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            await dbContextCache.Value.RollbackToSavepointAsync(name, cancellationToken);
        }
    }

    public void ReleaseSavepoint(string name)
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            dbContextCache.Value.ReleaseSavepoint(name);
        }
    }

    public async Task ReleaseSavepointAsync(string name,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            await dbContextCache.Value.ReleaseSavepointAsync(name, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var dbContextCache in _dbContextCaches)
        {
            await dbContextCache.Value.DisposeAsync();
        }

        _dbContextCaches.Clear();
    }

    /// <summary>
    /// 自动切换成写库连接
    /// </summary>
    private void AutoUseWriteConnectionString()
    {
        if (_virtualDatasource.ConnectionStringManager is ReadWriteConnectionStringManager)
        {
            ((IShardingDbContext) _shardingDbContext).ReadWriteSeparationWriteOnly();
        }
    }
}