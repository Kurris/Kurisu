using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;

namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations;

public class ReadWriteConnectionStringManager : IConnectionStringManager, IReadWriteConnectionStringManager
{
    private IShardingConnectionStringResolver _shardingConnectionStringResolver;
    private readonly IVirtualDatasource _virtualDataSource;


    public ReadWriteConnectionStringManager(IVirtualDatasource virtualDataSource, IReadWriteConnectorFactory readWriteConnectorFactory)
    {
        _virtualDataSource = virtualDataSource;
        var readWriteConnectors = virtualDataSource.ConfigurationParams.ReadWriteNodeSeparationConfigs.Select(o => readWriteConnectorFactory.CreateConnector(virtualDataSource.ConfigurationParams.ReadStrategy.GetValueOrDefault(), o.Key, o.Value));
        _shardingConnectionStringResolver = new ReadWriteShardingConnectionStringResolver(readWriteConnectors, virtualDataSource.ConfigurationParams.ReadStrategy.GetValueOrDefault(), readWriteConnectorFactory);
    }

    public string GetConnectionString(string dataSourceName)
    {
        return GetReadNodeConnectionString(dataSourceName, null);
    }

    public string GetReadNodeConnectionString(string dataSourceName, string readNodeName)
    {
        if (!_shardingConnectionStringResolver.ContainsReadWriteDataSourceName(dataSourceName))
            return _virtualDataSource.GetConnectionString(dataSourceName);
        return _shardingConnectionStringResolver.GetConnectionString(dataSourceName, readNodeName);
    }

    public bool AddReadConnectionString(string dataSourceName, string connectionString, string readNodeName)
    {
        return _shardingConnectionStringResolver.AddConnectionString(dataSourceName, connectionString, readNodeName);
    }
}