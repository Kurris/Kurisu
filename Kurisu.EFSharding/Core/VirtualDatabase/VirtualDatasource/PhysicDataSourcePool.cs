using System.Collections.Concurrent;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource.Abstractions;

namespace Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;

public sealed class PhysicDatasourcePool : IPhysicDatasourcePool
{
    private readonly ConcurrentDictionary<string, DatasourceUnit> _physicDataSources = new();

    public bool TryAdd(DatasourceUnit physicDataSource)
    {
        return _physicDataSources.TryAdd(physicDataSource.Name, physicDataSource);
    }

    public DatasourceUnit TryGet(string dataSourceName)
    {
        if (dataSourceName == null) return null;

        return _physicDataSources.TryGetValue(dataSourceName, out var physicDataSource)
            ? physicDataSource
            : null;
    }

    public List<string> GetAllDatasourceNames()
    {
        return _physicDataSources.Keys.ToList();
    }

    public IDictionary<string, string> GetDatasource()
    {
        return _physicDataSources.ToDictionary(k => k.Key, k => k.Value.ConnectionString);
    }
}