namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;

public interface IReadWriteConnectorFactory
{
    IReadWriteConnector CreateConnector(ReadStrategyEnum strategy, string dataSourceName,
        ReadNode[] readNodes);
}