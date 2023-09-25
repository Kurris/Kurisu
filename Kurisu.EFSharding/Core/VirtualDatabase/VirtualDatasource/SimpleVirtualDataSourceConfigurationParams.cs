using System.Data.Common;
using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.ShardingConfigurations;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource.Abstractions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;

internal class SimpleVirtualDataSourceConfigurationParams : AbstractVirtualDatasourceConfigurationParams
{
    private readonly ShardingOptions _options;
    public override int MaxQueryConnectionsLimit { get; }
    public override ConnectionModeEnum ConnectionMode { get; }
    public override List<DatasourceUnit> Datasources { get; }
    public override IDictionary<string, ReadNode[]> ReadWriteNodeSeparationConfigs { get; }
    public override ReadStrategyEnum? ReadStrategy { get; }
    public override ReadWriteDefaultEnableBehavior? ReadWriteDefaultEnableBehavior { get; }
    public override int? ReadWriteDefaultPriority { get; }
    public override ReadConnStringGetStrategyEnum? ReadConnStringGetStrategy { get; }

    public SimpleVirtualDataSourceConfigurationParams(IShardingProvider shardingProvider, ShardingOptions options)
    {
        _options = options;
        MaxQueryConnectionsLimit = options.MaxQueryConnectionsLimit;
        Datasources = options.DatasourceConfigure?.Invoke(shardingProvider) ?? new List<DatasourceUnit>();


        if (options.ShardingReadWriteSeparationOptions != null)
        {
            if (options.ShardingReadWriteSeparationOptions.ReadWriteNodeSeparationConfigure != null)
            {
                var readConfig = options.ShardingReadWriteSeparationOptions.ReadWriteNodeSeparationConfigure?.Invoke(shardingProvider);
                if (readConfig != null)
                {
                    ReadWriteNodeSeparationConfigs = readConfig.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
                }
            }
            else
            {
                var nodeConfig = options.ShardingReadWriteSeparationOptions.ReadWriteSeparationConfigure?.Invoke(shardingProvider);
                if (nodeConfig != null)
                {
                    ReadWriteNodeSeparationConfigs = nodeConfig.ToDictionary(kv => kv.Key,
                        kv => kv.Value.Select(o => new ReadNode(Guid.NewGuid().ToString("n"), o)).ToArray());
                }
            }

            ReadStrategy = options.ShardingReadWriteSeparationOptions.ReadStrategy;
            ReadWriteDefaultEnableBehavior = options.ShardingReadWriteSeparationOptions.DefaultEnableBehavior;
            ReadWriteDefaultPriority = options.ShardingReadWriteSeparationOptions.DefaultPriority;
            ReadConnStringGetStrategy = options.ShardingReadWriteSeparationOptions.ReadConnStringGetStrategy;
        }
    }

    public override DbContextOptionsBuilder UseDbContextOptionsBuilder(string connectionString,
        DbContextOptionsBuilder dbContextOptionsBuilder)
    {
        if (_options.ConnectionStringConfigure == null)
        {
            throw new InvalidOperationException($"unknown {nameof(UseDbContextOptionsBuilder)} by connection string");
        }

        _options.ConnectionStringConfigure.Invoke(connectionString, dbContextOptionsBuilder);
        return dbContextOptionsBuilder;
    }

    public override DbContextOptionsBuilder UseDbContextOptionsBuilder(DbConnection dbConnection,
        DbContextOptionsBuilder dbContextOptionsBuilder)
    {
        if (_options.ConnectionConfigure == null)
        {
            throw new InvalidOperationException($"unknown {nameof(UseDbContextOptionsBuilder)} by connection");
        }

        _options.ConnectionConfigure.Invoke(dbConnection, dbContextOptionsBuilder);
        return dbContextOptionsBuilder;
    }

    public override void UseShellDbContextOptionBuilder(DbContextOptionsBuilder dbContextOptionsBuilder)
    {
        _options.ShellDbContextConfigure?.Invoke(dbContextOptionsBuilder);
    }

    public override void UseExecutorDbContextOptionBuilder(DbContextOptionsBuilder dbContextOptionsBuilder)
    {
        _options.ExecutorDbContextConfigure?.Invoke(dbContextOptionsBuilder);
    }
}