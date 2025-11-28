using System.Data;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Extensions;
using Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

public sealed class SqlSugarDatasourceManager : AbstractDatasourceManager<ISqlSugarClient>
{
    private readonly Stack<ISqlSugarClient> _clients = new();
    private readonly Stack<Propagation> _propagations = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SqlSugarDatasourceManager> _logger;

    private readonly Action<bool> _onAfterTransScope;
    private readonly Action _onAfterScope;


    public int ClientCount => _clients.Count;
    public int PropagationCount => _propagations.Count;

    public SqlSugarDatasourceManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = _serviceProvider.GetService<ILogger<SqlSugarDatasourceManager>>();

        _logger.LogDebug("SqlSugarDatasourceManager initialized. Initial ClientCount={ClientCount}, PropagationCount={PropagationCount}", _clients.Count, _propagations.Count);

        _onAfterScope = () =>
        {
            //保存最后一个，避免栈空,用于后续数据库操作
            if (_clients.Count > 1)
            {
                _clients.Pop();
                _logger.LogDebug("Client popped after transaction end. Current client count: {ClientCount}", _clients.Count);
            }
        };

        _onAfterTransScope = pop =>
        {
            // capture counts for logging
            var prevPropCount = _propagations.Count;
            _propagations.Pop();
            var newPropCount = _propagations.Count;
            _logger.LogDebug("Transaction scope ended. pop={Pop}. PropagationCount: {Prev}-> {New}", pop, prevPropCount, newPropCount);

            if (!pop) return;

            _onAfterScope();
        };
    }

    public override IDisposable CreateScope(string name)
    {
        var dbConnectionManager = _serviceProvider.GetRequiredService<IDbConnectionManager>();
        if (!dbConnectionManager.NeedCreateScope(name))
        {
            return dbConnectionManager.CreateScope(name, null);
        }

        var scope = dbConnectionManager.CreateScope(name, () => _onAfterScope());
        _logger.LogDebug("Created new scope with name '{Name}'.", name);
        CreateClient();
        return scope;
    }

    public override ISqlSugarClient CreateClient()
    {
        var client = _serviceProvider.GetRequiredService<ISqlSugarClient>();
        _clients.Push(client);
        _logger.LogDebug("Created new Client and pushed to stack. ClientCount={ClientCount}", _clients.Count);
        return client;
    }


    // public override ISqlSugarClient CreateClient(string name)
    // {
    //     var dbConnectionManager = _serviceProvider.GetRequiredService<IDbConnectionManager>();
    //     using (dbConnectionManager.CreateScope(name))
    //     {
    //         var client = _serviceProvider.GetRequiredService<ISqlSugarClient>();
    //         _clients.Push(client);
    //         _logger.LogDebug("Created new Client with scope '{Name}' and pushed to stack. ClientCount={ClientCount}", name, _clients.Count);
    //         return _clients.Peek();
    //     }
    // }


    public override object GetCurrentClient()
    {
        var current = _clients.Count > 0 ? _clients.Peek() : null;
        if (current == null)
        {
            _logger.LogDebug("No current Client found. Creating a new client.");
            return CreateClient();
        }

        _logger.LogDebug("Returning current Client. ClientCount={ClientCount}", _clients.Count);
        return current;
    }

    public override ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null)
    {
        var scope = CreateTransScopeInternal(propagation, isolationLevel);
        _propagations.Push(propagation);
        return scope;
    }

    private ITransactionScope CreateTransScopeInternal(Propagation propagation, IsolationLevel? isolationLevel = null)
    {
        _logger.LogDebug("CreateTransScopeInternal called. propagation={Propagation}, isolationLevel={IsolationLevel}, clients={ClientCount}, propagations={PropagationCount}", propagation, isolationLevel, _clients.Count, _propagations.Count);

        switch (propagation)
        {
            case Propagation.Required:
            {
                var client = _clients.Count > 0
                    ? this.GetCurrentClient<ISqlSugarClient>()
                    : CreateClient();

                var hasTran = client.Ado.IsAnyTran();
                _logger.LogDebug("Creating RequiredTransactionScope. hasAmbientTransaction={HasTran}", hasTran);

                return new RequiredTransactionScope(client,
                    isolationLevel,
                    hasTran,
                    _onAfterTransScope
                );
            }
            case Propagation.RequiresNew:
            {
                var client = CreateClient();
                _logger.LogDebug("Creating RequiresNewTransactionScope. ClientCount={ClientCount}", _clients.Count);
                return new RequiresNewTransactionScope(client, isolationLevel, _onAfterTransScope);
            }
            case Propagation.Mandatory:
            {
                var client = _propagations.Count > 0
                    ? this.GetCurrentClient<ISqlSugarClient>()
                    : throw new InvalidOperationException("No existing transaction found for transaction marked with propagation 'Mandatory'.");

                _logger.LogDebug("Creating MandatoryTransactionScope. PropagationCount={PropagationCount}", _propagations.Count);

                return new MandatoryTransactionScope(client,
                    isolationLevel, _onAfterTransScope
                );
            }
            case Propagation.Nested:
            {
                // 如果没有 ambient transaction，则与 Required 行为一致（新建事务）
                var client = _clients.Count > 0 ? this.GetCurrentClient<ISqlSugarClient>() : CreateClient();
                var hasTran = client.Ado.IsAnyTran();

                if (!hasTran)
                {
                    // 无外层事务：像 Required 一样新建事务，并确保在 Dispose 时清理
                    _logger.LogDebug("No ambient transaction found for Nested propagation. Falling back to Required behavior.");
                    return new RequiredTransactionScope(client, isolationLevel, false, _onAfterTransScope);
                }

                // 有外层事务：使用 savepoint 实现嵌套事务
                _logger.LogDebug("Creating NestedTransactionScope. Ambient transaction exists.");
                return new NestedTransactionScope(client, isolationLevel, _onAfterTransScope);
            }
            case Propagation.Never:
            {
                // 如果当前存在任何 transaction propagation，则不允许在事务中运行
                if (_propagations.Count > 0)
                {
                    _logger.LogDebug("Propagation 'Never' requested but existing propagation detected. PropagationCount={PropagationCount}", _propagations.Count);
                    throw new InvalidOperationException("Existing transaction found for transaction marked with propagation 'Never'.");
                }

                // 无 ambient：按非事务方式执行（Begin/Commit/Rollback 都为 no-op）
                var client = _clients.Count > 0 ? this.GetCurrentClient<ISqlSugarClient>() : CreateClient();
                _logger.LogDebug("Creating NoTransactionScope. ClientCount={ClientCount}", _clients.Count);
                return new NoTransactionScope(client, isolationLevel, _onAfterTransScope);
            }

            default:
                throw new NotSupportedException($"Propagation {propagation} not supported.");
        }
    }
}