using System.Data.Common;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource.Abstractions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;

public class VirtualDatasource : IVirtualDatasource
{
    public IVirtualDatasourceConfigurationParams ConfigurationParams { get; }
    public IConnectionStringManager ConnectionStringManager { get; }


    private readonly IPhysicDatasourcePool _physicDatasourcePool;
    public string DefaultDatasourceName { get; private set; }
    public string DefaultConnectionString { get; private set; }
    public bool UseReadWriteSeparation { get; }

    public VirtualDatasource(IVirtualDatasourceConfigurationParams configurationParams, IReadWriteConnectorFactory readWriteConnectorFactory)
    {
        if (configurationParams.MaxQueryConnectionsLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(configurationParams.MaxQueryConnectionsLimit));
        ConfigurationParams = configurationParams;
        _physicDatasourcePool = new PhysicDatasourcePool();
        //添加数据源
        foreach (var datasource in ConfigurationParams.Datasources)
        {
            AddPhysicDatasource(new DatasourceUnit(datasource.IsDefault, datasource.Name, datasource.ConnectionString));
        }

        UseReadWriteSeparation = ConfigurationParams.UseReadWriteSeparation();
        if (UseReadWriteSeparation)
        {
            CheckReadWriteSeparation();
            ConnectionStringManager = new ReadWriteConnectionStringManager(this, readWriteConnectorFactory);
        }
        else
        {
            ConnectionStringManager = new DefaultConnectionStringManager(this);
        }
    }

    private void CheckReadWriteSeparation()
    {
        if (!ConfigurationParams.ReadStrategy.HasValue)
        {
            throw new ArgumentException(nameof(ConfigurationParams.ReadStrategy));
        }

        if (!ConfigurationParams.ReadConnStringGetStrategy.HasValue)
        {
            throw new ArgumentException(nameof(ConfigurationParams.ReadConnStringGetStrategy));
        }

        if (!ConfigurationParams.ReadWriteDefaultEnableBehavior.HasValue)
        {
            throw new ArgumentException(nameof(ConfigurationParams.ReadWriteDefaultEnableBehavior));
        }

        if (!ConfigurationParams.ReadWriteDefaultPriority.HasValue)
        {
            throw new ArgumentException(nameof(ConfigurationParams.ReadWriteDefaultPriority));
        }
    }

    /// <summary>
    /// 获取默认数据源
    /// </summary>
    /// <returns></returns>
    public DatasourceUnit GetDefaultDatasource()
    {
        return GetPhysicDatasource(DefaultDatasourceName);
    }

    /// <summary>
    /// 获取物理数据源
    /// </summary>
    /// <param name="datasourceName"></param>
    /// <returns></returns>
    /// <exception cref="ShardingCoreNotFoundException"></exception>
    public DatasourceUnit GetPhysicDatasource(string datasourceName)
    {
        var datasource = _physicDatasourcePool.TryGet(datasourceName);
        if (null == datasource)
            throw new ShardingCoreNotFoundException($"data source:[{datasourceName}]");

        return datasource;
    }

    /// <summary>
    /// 获取所有的数据源名称
    /// </summary>
    /// <returns></returns>
    public List<string> GetAllDatasourceNames()
    {
        return _physicDatasourcePool.GetAllDatasourceNames();
    }

    /// <summary>
    /// 根据数据源名称获取链接字符串
    /// </summary>
    /// <param name="datasourceName"></param>
    /// <returns></returns>
    /// <exception cref="ShardingCoreNotFoundException"></exception>
    public string GetConnectionString(string datasourceName)
    {
        return IsDefault(datasourceName)
            ? DefaultConnectionString
            : GetPhysicDatasource(datasourceName).ConnectionString;
    }


    public bool AddPhysicDatasource(DatasourceUnit physicDatasource)
    {
        if (physicDatasource.IsDefault)
        {
            if (!string.IsNullOrWhiteSpace(DefaultDatasourceName))
            {
                throw new ShardingCoreInvalidOperationException($"default data source name:[{DefaultDatasourceName}],add physic default data source name:[{physicDatasource.Name}]");
            }

            DefaultDatasourceName = physicDatasource.Name;
            DefaultConnectionString = physicDatasource.ConnectionString;
        }

        return _physicDatasourcePool.TryAdd(physicDatasource);
    }

    /// <summary>
    /// 判断数据源名称是否是默认的数据源
    /// </summary>
    /// <param name="datasourceName"></param>
    /// <returns></returns>
    public bool IsDefault(string datasourceName)
    {
        return DefaultDatasourceName == datasourceName;
    }

    /// <summary>
    /// 检查虚拟数据源是否包含默认值
    /// </summary>
    /// <exception cref="ShardingCoreInvalidOperationException"></exception>
    public void CheckVirtualDatasource()
    {
        if (string.IsNullOrWhiteSpace(DefaultDatasourceName))
            throw new ShardingCoreInvalidOperationException(
                $"virtual data source not inited {nameof(DefaultDatasourceName)} in IShardingDbContext null");
        if (string.IsNullOrWhiteSpace(DefaultConnectionString))
            throw new ShardingCoreInvalidOperationException(
                $"virtual data source not inited {nameof(DefaultConnectionString)} in IShardingDbContext null");
    }

    public DbContextOptionsBuilder UseDbContextOptionsBuilder(string connectionString,
        DbContextOptionsBuilder dbContextOptionsBuilder)
    {
        var doUseDbContextOptionsBuilder = ConfigurationParams.UseDbContextOptionsBuilder(connectionString, dbContextOptionsBuilder);
        doUseDbContextOptionsBuilder.UseInnerDbContextSharding();
        ConfigurationParams.UseExecutorDbContextOptionBuilder(dbContextOptionsBuilder);
        return doUseDbContextOptionsBuilder;
    }

    public DbContextOptionsBuilder UseDbContextOptionsBuilder(DbConnection dbConnection,
        DbContextOptionsBuilder dbContextOptionsBuilder)
    {
        var doUseDbContextOptionsBuilder = ConfigurationParams.UseDbContextOptionsBuilder(dbConnection, dbContextOptionsBuilder);
        doUseDbContextOptionsBuilder.UseInnerDbContextSharding();
        ConfigurationParams.UseExecutorDbContextOptionBuilder(dbContextOptionsBuilder);
        return doUseDbContextOptionsBuilder;
    }

    public IDictionary<string, string> GetDatasource()
    {
        return _physicDatasourcePool.GetDatasource();
    }
}