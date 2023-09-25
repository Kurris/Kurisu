namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;

public interface IReadWriteConnectionStringManager
{
    string GetReadNodeConnectionString(string dataSourceName,string readNodeName);
    bool AddReadConnectionString(string dataSourceName,string connectionString, string readNodeName);
}