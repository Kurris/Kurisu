using System.Data;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

/// <summary>
/// 数据源管理器
/// </summary>
public sealed class SqlSugarDatasourceManager : AbstractDatasourceManager<ISqlSugarClient>
{
    private readonly NameClientCollection _nameClientCollection = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ISqlSugarClient> _logger;
    private readonly IDbConnectionStringManager _dbConnectionManager;
    private int _newClientIndex = 0;

    /// <summary>
    /// 当前数据源名称：优先取实例自身栈顶，栈为空时回退到默认数据源
    /// </summary>
    public string Current => _dbConnectionManager.Current;

    public int ClientCount => _nameClientCollection.Count;

    public SqlSugarDatasourceManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetService<ILogger<ISqlSugarClient>>();
        _dbConnectionManager = serviceProvider.GetService<IDbConnectionStringManager>();
        CreateClient(Current);
    }

    public override TClientDefined GetCurrentClient<TClientDefined>()
    {
        var client = GetCurrentClient();
        if (client is TClientDefined typedClient)
        {
            return typedClient;
        }

        throw new InvalidCastException($"无法将当前客户端转换为类型 {typeof(TClientDefined).FullName}.");
    }

    public override ISqlSugarClient GetCurrentClient()
    {
        return _nameClientCollection.GetClient(Current);
    }

    public override ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null)
    {
        return CreateTransScopeInternal(propagation, isolationLevel);
    }

    public ISqlSugarClient CreateClient(string name)
    {
        var client = _serviceProvider.GetService<ISqlSugarClient>();
        _nameClientCollection.TryAddClient(name, client);
        _logger.LogDebug("已为数据源'{Name}'创建客户端.", name);
        return client;
    }

    private ITransactionScope CreateTransScopeInternal(Propagation propagation, IsolationLevel? isolationLevel = null)
    {
        switch (propagation)
        {
            case Propagation.Required:
                {
                    var client = GetCurrentClient();
                    var hasTran = client.Ado.IsAnyTran();

                    return new RequiredTransactionScope(client, isolationLevel, hasTran, () =>
                        _logger.LogDebug("Required 事务作用域结束. 当前数据源='{Current}'", Current));
                }
            case Propagation.RequiresNew:
                {
                    // 为独立事务创建一个专属的新客户端（不复用当前连接，确保事务隔离）
                    _newClientIndex++;
                    var newName = $"{Current}_New{_newClientIndex}";
                    var d = _dbConnectionManager.CreateScope(newName);
                    var client = CreateClient(newName);
                    return new RequiresNewTransactionScope(client, isolationLevel, () =>
                    {
                        _logger.LogDebug("RequiresNew 事务作用域结束. 当前数据源='{Current}'", Current);
                        _newClientIndex--;
                        _nameClientCollection.Release(newName);
                        d.Dispose();
                    });
                }
            case Propagation.Mandatory:
                {
                    var client = GetCurrentClient();
                    if (!client.Ado.IsAnyTran())
                    {
                        throw new InvalidOperationException("当前事务传播性为'Mandatory'，请确保调用链中存在事务.");
                    }

                    return new MandatoryTransactionScope(client, isolationLevel,
                        () => _logger.LogDebug("Mandatory 事务作用域结束. 当前数据源='{Current}'", Current));
                }
            case Propagation.Nested:
                {
                    var client = GetCurrentClient();
                    var hasTran = client.Ado.IsAnyTran();

                    if (!hasTran)
                    {
                        return new RequiredTransactionScope(client, isolationLevel, false,
                            () => _logger.LogDebug("Nested(降级为Required) 事务作用域结束. 当前数据源='{Current}'", Current));
                    }

                    return new NestedTransactionScope(client, isolationLevel,
                        () => _logger.LogDebug("Nested 事务作用域结束. 当前数据源='{Current}'", Current));
                }
            case Propagation.Never:
                {
                    var client = GetCurrentClient();
                    if (client.Ado.IsAnyTran())
                    {
                        throw new InvalidOperationException("当前事务传播性标记为'Never'，不允许在已有事务的作用域中执行.");
                    }

                    _logger.LogDebug("创建 NoTransactionScope. 客户端个数={ClientCount}", _nameClientCollection.Count);
                    return new NoTransactionScope(client, isolationLevel,
                        () => _logger.LogDebug("Never 事务作用域结束. 当前数据源='{Current}'", Current));
                }

            default:
                throw new NotSupportedException($"事务传播性 {propagation} 不支持.");
        }
    }
}