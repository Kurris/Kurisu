using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.Utils.Disposables;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Options;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

/// <summary>
/// 数据库连接处理
/// </summary>
/// <remarks>
/// ctor
/// </remarks>
/// <param name="dbConnectionStringRegistry"></param>
/// <param name="logger"></param>
internal class SqlSugarConnectionStringManager(
    IDbConnectionRegistry dbConnectionStringRegistry,
    IContextAccessor<NamesDbConnectionStringStack> namesAccessor,
    ILogger<SqlSugarConnectionStringManager> logger) : IDbConnectionStringManager
{

    public IDisposable CreateScope(string name, Action onDispose)
    {
        return InternalCreateScope(name, onDispose);
    }

    public bool NeedCreateScope(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("数据源名称不能为空", nameof(name));

        if (!dbConnectionStringRegistry.Exists(name))
        {
            throw new ArgumentException(name + "连接字符串不存在");
        }

        // 如果当前名称和栈顶名称相同，则不需要创建新作用域
        return !name.Equals(Current);
    }

    public string GetConnectionString(string name)
    {
        return dbConnectionStringRegistry.GetConnectionString(name);
    }

    public string Current
    {
        get
        {
            if (namesAccessor.Current.Names.TryPeek(out var currentName))
            {
                return currentName;
            }
            else
            {
                return nameof(DbOptions.DefaultConnectionString);
            }
        }
    }

    public string GetCurrentConnectionString()
    {
        return GetConnectionString(Current);
    }

    public IDisposable CreateScope(string name)
    {
        return InternalCreateScope(name, null);
    }

    public IDisposable InternalCreateScope(string name, Action onDispose)
    {
        try
        {
            if (!NeedCreateScope(name))
            {
                return new NoopScope();
            }

            namesAccessor.Current.Names.Push(name);
            return new ActionScope(() =>
            {
                PopIfNeeded();
                onDispose?.Invoke();
            });
        }
        finally
        {
            logger.LogDebug("使用数据源'{CurrentName}'", name);

        }
    }

    private void PopIfNeeded()
    {
        namesAccessor.Current.Names.Pop();
    }
}

internal class NamesDbConnectionStringStack : IContextable<NamesDbConnectionStringStack>
{
    public NamesDbConnectionStringStack()
    {
        Names = new Stack<string>();
    }

    public Stack<string> Names { get; set; }
}