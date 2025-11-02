using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.SqlSugar.Options;
using Microsoft.Extensions.Options;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

/// <summary>
/// Db数据库连接处理
/// </summary>
internal class SqlSugarConnectionManager : IDbConnectionManager
{
    private readonly IDbConnectionRegistry _dbConnectionRegistry;
    private readonly AsyncLocal<Stack<string>> _nameStack = new();
    private readonly string _default = "default";

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="dbConnectionRegistry"></param>
    public SqlSugarConnectionManager(IOptions<SqlSugarOptions> options, IDbConnectionRegistry dbConnectionRegistry)
    {
        _dbConnectionRegistry = dbConnectionRegistry;
        dbConnectionRegistry.Register(_default, options.Value.DefaultConnectionString);
    }

    // Ensure there is a stack for the current async context and initialize with default if empty
    private Stack<string> Stack
    {
        get
        {
            if (_nameStack.Value == null)
            {
                _nameStack.Value = new Stack<string>();
                _nameStack.Value.Push(_default);
            }

            return _nameStack.Value;
        }
    }

    public string GetConnectionString(string name)
    {
        return _dbConnectionRegistry.GetConnectionString(name);
    }

    public string GetCurrent()
    {
        return Stack.Peek();
    }

    public string GetCurrentConnectionString()
    {
        return GetConnectionString(GetCurrent());
    }

    public IDisposable CreateScope(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name cannot be null or empty", nameof(name));

        if (!_dbConnectionRegistry.Exists(name))
        {
            throw new ArgumentException(nameof(name) + "连接字符串不存在");
        }

        // If it's the same as current, return a no-op disposable
        if (name.Equals(GetCurrent()))
        {
            return new NoopScope();
        }

        Stack.Push(name);
        return new NameScope(this);
    }

    private void PopIfNeeded()
    {
        if (Stack.Count > 1)
        {
            Stack.Pop();
        }
    }

    private class NameScope : IDisposable
    {
        private readonly SqlSugarConnectionManager _manager;
        private bool _disposed;

        public NameScope(SqlSugarConnectionManager manager)
        {
            _manager = manager;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _manager.PopIfNeeded();
        }
    }

    private class NoopScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}