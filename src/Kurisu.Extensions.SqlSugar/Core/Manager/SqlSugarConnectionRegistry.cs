using Kurisu.AspNetCore.Abstractions.DataAccess.Core;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

internal class SqlSugarConnectionRegistry : IDbConnectionRegistry
{
    private readonly Dictionary<string, string> _connectionStrings = new();

    public void Register(Dictionary<string, string> connectionStrings)
    {
        foreach (var kvp in connectionStrings)
        {
            _connectionStrings[kvp.Key] = kvp.Value;
        }
    }

    public void Register(string name, string connectionString)
    {
        _connectionStrings[name] = connectionString;
    }

    public string GetConnectionString(string name)
    {
        if (_connectionStrings.TryGetValue(name, out var connectionString))
        {
            return connectionString;
        }

        throw new KeyNotFoundException($"Connection string with name '{name}' not found.");
    }

    public bool Exists(string name)
    {
        return _connectionStrings.ContainsKey(name);
    }
}