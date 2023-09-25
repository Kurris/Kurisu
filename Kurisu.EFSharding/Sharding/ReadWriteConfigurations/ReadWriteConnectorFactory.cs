using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Connectors;

namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations;

public class ReadWriteConnectorFactory: IReadWriteConnectorFactory
{
    public IReadWriteConnector CreateConnector(ReadStrategyEnum strategy,string dataSourceName, ReadNode[] readNodes)
    {

        if (strategy == ReadStrategyEnum.Loop)
        {
            return new ReadWriteLoopConnector(dataSourceName, readNodes);
        }
        else if (strategy == ReadStrategyEnum.Random)
        {
            return new ReadWriteRandomConnector(dataSourceName, readNodes);
        }
        else
        {
            throw new ShardingCoreInvalidOperationException(
                $"unknown read write strategy:[{strategy}]");
        }
    }
}