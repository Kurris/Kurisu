using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Options;
using Microsoft.Extensions.Options;

namespace Kurisu.Extensions.SqlSugar.Services.Implements;

/// <summary>
/// Db数据库连接处理
/// </summary>
public class SqlSugarConnectionManager : IDbConnectionManager
{
    private string _before = string.Empty;
    private string _current = "default";
    private readonly Dictionary<string, string> _connectionStrings = new();

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    public SqlSugarConnectionManager(IOptions<SqlSugarOptions> options)
    {
        _connectionStrings.Add(_current, options.Value.DefaultConnectionString);
    }

    public async Task SwitchConnectionStringAsync(string name, Func<Task> todo)
    {
        if (name.Equals(_current)) return;

        if (!_connectionStrings.ContainsKey(name))
        {
            throw new ArgumentException(nameof(name) + "连接字符串不存在");
        }

        _before = _current;
        _current = name;

        try
        {
            await todo();
        }
        finally
        {
            //还原
            (_before, _current) = (_current, _before);
        }
    }

    public string GetConnectionString(string name)
    {
        return _connectionStrings[name];
    }

    public string GetCurrent()
    {
        return _current;
    }

    public void SwitchConnectionString(string name, Action todo)
    {
        if (name.Equals(_current)) return;

        if (!_connectionStrings.ContainsKey(name))
        {
            throw new ArgumentException(nameof(name) + "连接字符串不存在");
        }

        _before = _current;
        _current = name;

        try
        {
            todo();
        }
        finally
        {
            //还原
            (_before, _current) = (_current, _before);
        }
    }

    public string GetCurrentConnectionString()
    {
        return GetConnectionString(GetCurrent());
    }

    public void Register(string name, string connectionString)
    {
        if (!_connectionStrings.TryAdd(name, connectionString))
        {
            throw new ArgumentException(nameof(name) + "连接字符串已存在");
        }
    }
}