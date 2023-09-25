using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;
using Kurisu.EFSharding.Sharding.Abstractions;

namespace Kurisu.EFSharding.Sharding;

public class DefaultConnectionStringManager : IConnectionStringManager
{
    private readonly IVirtualDatasource _virtualDataSource;

    public DefaultConnectionStringManager(IVirtualDatasource virtualDataSource)
    {
        _virtualDataSource = virtualDataSource;
    }

    public string GetConnectionString(string dataSourceName)
    {
        return _virtualDataSource.IsDefault(dataSourceName)
            ? _virtualDataSource.DefaultConnectionString
            : _virtualDataSource.GetPhysicDatasource(dataSourceName).ConnectionString;
    }
}