using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Helpers;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Connectors.Abstractions;

namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Connectors;

public class ReadWriteRandomConnector:AbstractionReadWriteConnector
{
    public ReadWriteRandomConnector(string dataSourceName,ReadNode[] readNodes):base(dataSourceName, readNodes)
    {
    }

    private string DoGetNoReadNameConnectionString()
    {
        if (Length == 1)
            return ReadNodes[0].ConnectionString;
        var next = RandomHelper.Next(0, Length);
        return ReadNodes[next].ConnectionString;
    }

    public override string DoGetConnectionString(string readNodeName)
    {
        if (readNodeName == null)
        {
            return DoGetNoReadNameConnectionString();
        }else
        {
            return ReadNodes.FirstOrDefault(o => o.Name == readNodeName)?.ConnectionString ??
                   throw new ShardingCoreInvalidOperationException($"read node name :[{readNodeName}] not found");
        }
    }
}