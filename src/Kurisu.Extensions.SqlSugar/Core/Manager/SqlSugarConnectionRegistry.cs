using Kurisu.AspNetCore.Abstractions.DataAccess.Core;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

internal class SqlSugarConnectionRegistry : IDbConnectionRegistry
{
    private readonly Dictionary<string, string> _connectionStrings = new();

    public void Register(Dictionary<string, string> connectionStrings)
    {
        if (connectionStrings == null) throw new ArgumentNullException(nameof(connectionStrings));

        foreach (var kvp in connectionStrings)
        {
            _connectionStrings[kvp.Key] = kvp.Value;
        }
    }

    public void Register(string name, string connectionString)
    {
        if (Exists(name))
        {
            throw new InvalidDataException($"重复注册'{name}'连接字符串");
        }

        _connectionStrings[name] = connectionString;
    }

    public string GetConnectionString(string name)
    {
        var realName = GetRealName(name);
        if (_connectionStrings.TryGetValue(realName, out var connectionString))
        {
            return connectionString;
        }

        throw new KeyNotFoundException($"找不到名为'{name}'的连接字符串");
    }

    public bool Exists(string name)
    {
        var realName = GetRealName(name);
        return _connectionStrings.ContainsKey(realName);
    }

    /// <summary>
    /// 获取实际名称,去除可能的后缀
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static string GetRealName(string name)
    {
        return name.Split('_', StringSplitOptions.RemoveEmptyEntries)[0];
    }
}