using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Connectors.Abstractions;

namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Connectors;

public class ReadWriteLoopConnector: AbstractionReadWriteConnector
{
    private long _seed = 0;
    public ReadWriteLoopConnector(string dataSourceName, ReadNode[] readNodes) :base(dataSourceName, readNodes)
    {
    }

    private  string DoGetNoReadNameConnectionString()
    {
        if (Length == 1)
            return ReadNodes[0].ConnectionString;
        var newValue = Interlocked.Increment(ref _seed);
        var next = (int)(newValue % Length);
        if (next < 0)
            return ReadNodes[Math.Abs(next)].ConnectionString;
        return ReadNodes[next].ConnectionString;
    }

    public override string DoGetConnectionString(string readNodeName)
    {
        if (readNodeName == null)
        {
            return DoGetNoReadNameConnectionString();
        }
        else
        {
            return ReadNodes.FirstOrDefault(o => o.Name == readNodeName)?.ConnectionString ??
                   throw new ShardingCoreInvalidOperationException($"read node name :[{readNodeName}] not found");
        }
    }
}