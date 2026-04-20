using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

internal class NameClientCollection
{
    private readonly Dictionary<string, ISqlSugarClient> _clients = [];

    public void TryAddClient(string name, ISqlSugarClient client)
    {
        _clients.TryAdd(name, client);
    }

    public ISqlSugarClient GetClient(string name)
    {
        if (_clients.TryGetValue(name, out var client))
        {
            return client;
        }
        throw new KeyNotFoundException($"No SqlSugarClient found with the name '{name}'.");
    }

    public bool Exists(string name)
    {
        return _clients.ContainsKey(name);
    }

    public void Release(string name)
    {
        _clients.Remove(name);
    }

    public int Count => _clients.Count;
}
