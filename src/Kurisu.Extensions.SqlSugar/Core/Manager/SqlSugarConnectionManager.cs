using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.SqlSugar.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

/// <summary>
/// Db数据库连接处理
/// </summary>
internal class SqlSugarConnectionManager : IDbConnectionManager
{
    private readonly IDbConnectionRegistry _dbConnectionRegistry;
    private readonly ILogger<SqlSugarConnectionManager> _logger;
    private readonly AsyncLocal<Stack<string>> _nameStack = new();
    private readonly string _default = "default";

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="dbConnectionRegistry"></param>
    /// <param name="logger"></param>
    public SqlSugarConnectionManager(IOptions<SqlSugarOptions> options, IDbConnectionRegistry dbConnectionRegistry, ILogger<SqlSugarConnectionManager> logger)
    {
        _dbConnectionRegistry = dbConnectionRegistry;
        _logger = logger;
        dbConnectionRegistry.Register(_default, options.Value.DefaultConnectionString);
    }

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

    public IDisposable CreateScope(string name, Action onDispose)
    {
        return InternalCreateScope(name, onDispose);
    }

    public bool NeedCreateScope(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name cannot be null or empty", nameof(name));

        if (!_dbConnectionRegistry.Exists(name))
        {
            throw new ArgumentException(nameof(name) + "连接字符串不存在");
        }

        // 如果当前名称和栈顶名称相同，则不需要创建新作用域
        return !name.Equals(GetCurrent());
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
        return InternalCreateScope(name, null);
    }

    public IDisposable InternalCreateScope(string name, Action onDispose)
    {
        if (!NeedCreateScope(name))
        {
            return new NoopScope();
        }

        try
        {
            Stack.Push(name);
            return new NameScope(this, onDispose);
        }
        finally
        {
            _logger.LogDebug("Switched database connection to '{Name}'", name);
        }
    }

    private void PopIfNeeded()
    {
        if (Stack.Count > 1)
        {
            Stack.Pop();
        }

        _logger.LogDebug("Reverted database connection to '{Name}'", GetCurrent());
    }

    private class NameScope : IDisposable
    {
        private readonly SqlSugarConnectionManager _manager;
        private readonly Action _additionalDisposeAction;
        private bool _disposed;

        public NameScope(SqlSugarConnectionManager manager, Action additionalDisposeAction = null)
        {
            _manager = manager;
            _additionalDisposeAction = additionalDisposeAction;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _manager.PopIfNeeded();

            _additionalDisposeAction?.Invoke();
        }
    }

    private class NoopScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}