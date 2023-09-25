using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding;

public class ActualConnectionStringManager
{
    public DbContext ShellDbContext { get; }

    private readonly bool _useReadWriteSeparation;

    private readonly IShardingReadWriteManager _shardingReadWriteManager;

    private readonly IVirtualDatasource _virtualDataSource;
    public int ReadWriteSeparationPriority { get; set; }
    public ReadWriteDefaultEnableBehavior ReadWriteSeparation { get; set; }
    public ReadStrategyEnum ReadStrategy { get; }
    public ReadConnStringGetStrategyEnum ReadConnStringGetStrategy { get; }

    private string _cacheConnectionString;

    public ActualConnectionStringManager(IShardingReadWriteManager shardingReadWriteManager, IVirtualDatasource virtualDataSource, DbContext shellDbContext)
    {
        ShellDbContext = shellDbContext;
        _shardingReadWriteManager = shardingReadWriteManager;
        _virtualDataSource = virtualDataSource;
        _useReadWriteSeparation = virtualDataSource.ConnectionStringManager is ReadWriteConnectionStringManager;
        if (_useReadWriteSeparation)
        {
            ReadWriteSeparationPriority = virtualDataSource.ConfigurationParams.ReadWriteDefaultPriority.GetValueOrDefault();
            ReadWriteSeparation = virtualDataSource.ConfigurationParams.ReadWriteDefaultEnableBehavior.GetValueOrDefault(ReadWriteDefaultEnableBehavior.DefaultDisable);
            ReadStrategy = virtualDataSource.ConfigurationParams.ReadStrategy.GetValueOrDefault();
            ReadConnStringGetStrategy = virtualDataSource.ConfigurationParams.ReadConnStringGetStrategy.GetValueOrDefault();
        }
    }


    public string GetConnectionString(string datasourceName, bool isWrite)
    {
        if (isWrite)
            return GetWriteConnectionString(datasourceName);

        return _useReadWriteSeparation
            ? GetReadWriteSeparationConnectString(datasourceName)
            : _virtualDataSource.ConnectionStringManager.GetConnectionString(datasourceName);
    }

    private string GetWriteConnectionString(string datasourceName)
    {
        return _virtualDataSource.GetConnectionString(datasourceName);
    }

    private static bool UseReadWriteSeparation(ReadWriteDefaultEnableBehavior behavior, bool inTransaction)
    {
        return behavior switch
        {
            ReadWriteDefaultEnableBehavior.DefaultEnable => true,
            ReadWriteDefaultEnableBehavior.OutTransactionEnable => !inTransaction,
            _ => false
        };
    }

    private string GetReadWriteSeparationConnectString(string dataSourceName)
    {
        var inTransaction = ShellDbContext.Database.CurrentTransaction != null;
        var support = UseReadWriteSeparation(ReadWriteSeparation, inTransaction);
        string readNodeName = null;
        var hasConfig = false;
        var shardingReadWriteContext = _shardingReadWriteManager.GetCurrent();
        if (shardingReadWriteContext != null)
        {
            var dbFirst = ReadWriteSeparationPriority >= shardingReadWriteContext.DefaultPriority;
            support = dbFirst
                ? UseReadWriteSeparation(ReadWriteSeparation, inTransaction)
                : UseReadWriteSeparation(shardingReadWriteContext.DefaultEnableBehavior, inTransaction);
            if (!dbFirst && support)
            {
                hasConfig = shardingReadWriteContext.TryGetDataSourceReadNode(dataSourceName, out readNodeName);
            }
        }

        return support
            ? GetReadWriteSeparationConnectString0(dataSourceName, hasConfig
                ? readNodeName
                : null)
            : GetWriteConnectionString(dataSourceName);
    }

    private string GetReadWriteSeparationConnectString0(string datasourceName, string readNodeName)
    {
        if (_virtualDataSource.ConnectionStringManager is IReadWriteConnectionStringManager
            readWriteConnectionStringManager)
        {
            return ReadConnStringGetStrategy switch
            {
                ReadConnStringGetStrategyEnum.LatestFirstTime => _cacheConnectionString ??= readWriteConnectionStringManager.GetReadNodeConnectionString(datasourceName, readNodeName),
                ReadConnStringGetStrategyEnum.LatestEveryTime => readWriteConnectionStringManager.GetReadNodeConnectionString(datasourceName, readNodeName),
                _ => throw new ShardingCoreInvalidOperationException($"ReadWriteConnectionStringManager ReadConnStringGetStrategy:{ReadConnStringGetStrategy}")
            };
        }

        throw new ShardingCoreInvalidOperationException($"virtual data source connection string manager is not [{nameof(IReadWriteConnectionStringManager)}]");
    }
}