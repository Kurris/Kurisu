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