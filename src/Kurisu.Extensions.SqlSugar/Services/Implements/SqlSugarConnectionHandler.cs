using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Options;
using Microsoft.Extensions.Options;

namespace Kurisu.Extensions.SqlSugar.Services.Implements;

/// <summary>
/// Db数据库连接处理
/// </summary>
public class SqlSugarConnectionHandler : IDbConnectionManager
{
    private string _before = string.Empty;
    private string _current = "default";
    private readonly Dictionary<string, string> _connectionStrings = new();

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    public SqlSugarConnectionHandler(IOptions<SqlSugarOptions> options)
    {
        _connectionStrings.Add(_current, options.Value.DefaultConnectionString);
    }

    public string GetConnectionString(string name)
    {
        return _connectionStrings[name];
    }

    public string GetCurrent()
    {
        return _current;
    }

    public string GetCurrentConnectionString()
    {
        return GetConnectionString(GetCurrent());
    }

    public void Switch(string name)
    {
        if (name.Equals(_current)) return;

        if (!_connectionStrings.ContainsKey(name))
        {
            throw new ArgumentException(nameof(name) + "连接字符串不存在");
        }

        _before = _current;
        _current = name;
    }

    public void Switch()
    {
        //交换
        (_before, _current) = (_current, _before);
    }

    public void Register(string name, string connectionString)
    {
        if (!_connectionStrings.TryAdd(name, connectionString))
        {
            throw new ArgumentException(nameof(name) + "连接字符串已存在");
        }
    }
}