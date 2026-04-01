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

    private readonly Action _onAfterTransScope;
    private readonly Action _onDatesourceAfterScope;

    private int _newClientIndex = 0;


    private readonly IDbConnectionStringManager _dbConnectionManager;

    public int ClientCount => _nameClientCollection.Count;

    public SqlSugarDatasourceManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = _serviceProvider.GetService<ILogger<ISqlSugarClient>>();
        _dbConnectionManager = _serviceProvider.GetService<IDbConnectionStringManager>();

        _onDatesourceAfterScope = () =>
        {
            _logger.LogDebug("数据源作用域结束. 当前连接作用域为'{Current}'", _dbConnectionManager.Current);
        };

        _onAfterTransScope = () =>
        {
            _onDatesourceAfterScope();
        };
    }

    public override IDisposable CreateScope(string name)
    {
        try
        {
            //通过当前数据源判断是否需要切换数据源
            if (!_dbConnectionManager.NeedCreateScope(name))
            {
                _logger.LogDebug("当前连接作用域为'{Name}',无需创建.", name);
                return _dbConnectionManager.CreateScope(name, null);
            }

            return _dbConnectionManager.CreateScope(name, () => _onDatesourceAfterScope());
        }
        finally
        {
            if (!_nameClientCollection.Exists(name))
            {
                var client = _serviceProvider.GetService<ISqlSugarClient>();
                _nameClientCollection.TryAddClient(name, client);
            }
        }
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
        var name = _dbConnectionManager.Current;
        return _nameClientCollection.GetClient(name);
    }

    public override ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null)
    {
        var scope = CreateTransScopeInternal(propagation, isolationLevel);
        return scope;
    }


    private ITransactionScope CreateTransScopeInternal(Propagation propagation, IsolationLevel? isolationLevel = null)
    {
        var isolationLevelName = isolationLevel.HasValue ? isolationLevel.Value.ToString() : "Default";
        switch (propagation)
        {
            case Propagation.Required:
                {
                    var client = GetCurrentClient();
                    var hasTran = client.Ado.IsAnyTran();

                    return new RequiredTransactionScope(client,
                        isolationLevel,
                        hasTran,
                        _onAfterTransScope
                    );
                }
            case Propagation.RequiresNew:
                {
                    _newClientIndex++;
                    var name = $"{_dbConnectionManager.Current}_New{_newClientIndex}";
                    var tempScope = this.CreateScope(name);
                    var client = GetCurrentClient();
                    return new RequiresNewTransactionScope(client, isolationLevel, () =>
                    {
                        _onAfterTransScope();
                        tempScope.Dispose();
                        _newClientIndex--;
                    });
                }
            case Propagation.Mandatory:
                {
                    var client = GetCurrentClient();
                    var hasTrans = client.Ado.IsAnyTran();
                    if (!hasTrans)
                    {
                        throw new InvalidOperationException("当前事务传播性为'Mandatory',请确保调用链中存在事务.");
                    }

                    return new MandatoryTransactionScope(client,
                        isolationLevel, _onAfterTransScope
                    );
                }
            case Propagation.Nested:
                {
                    var client = GetCurrentClient();
                    var hasTran = client.Ado.IsAnyTran();

                    if (!hasTran)
                    {
                        return new RequiredTransactionScope(client, isolationLevel, false, _onAfterTransScope);
                    }

                    return new NestedTransactionScope(client, isolationLevel, _onAfterTransScope);
                }
            case Propagation.Never:
                {
                    // 无 ambient：按非事务方式执行（Begin/Commit/Rollback 都为 no-op）
                    var client = GetCurrentClient();
                    var hasTrans = client.Ado.IsAnyTran();
                    if (hasTrans)
                    {
                        throw new InvalidOperationException($"当前事务传播性标记为'Never',不允许执行作用域中开启事务");
                    }

                    _logger.LogDebug("创建NoTransactionScope. 客户端个数={ClientCount}", _nameClientCollection.Count);
                    return new NoTransactionScope(client, isolationLevel, _onAfterTransScope);
                }

            default:
                throw new NotSupportedException($"事务传播性 {propagation} 不支持.");
        }
    }
}