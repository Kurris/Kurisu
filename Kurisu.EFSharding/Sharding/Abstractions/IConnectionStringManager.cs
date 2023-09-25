namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IConnectionStringManager
{
    string GetConnectionString(string dataSourceName);
}