using System.Data;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Extensions;
using Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager;

public sealed class SqlSugarDatasourceManager : AbstractDatasourceManager<ISqlSugarClient>
{
    private readonly Stack<ISqlSugarClient> _clients = new();
    private readonly Stack<Propagation> _propagations = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly Action<bool> _onAfterScope;

    public int ClientCount => _clients.Count;
    public int PropagationCount => _propagations.Count;

    public SqlSugarDatasourceManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _onAfterScope = pop =>
        {
            _propagations.Pop();

            if (!pop) return;

            //保存最后一个，避免栈空,用于后续数据库操作
            if (_clients.Count > 1)
            {
                _clients.Pop();
            }
        };
    }

    public override ISqlSugarClient CreateClient()
    {
        var client = _serviceProvider.GetRequiredService<ISqlSugarClient>();
        _clients.Push(client);
        return client;
    }

    public override ISqlSugarClient CreateClient(string name)
    {
        var dbConnectionManager = _serviceProvider.GetRequiredService<IDbConnectionManager>();
        using (dbConnectionManager.CreateScope(name))
        {
            var client = _serviceProvider.GetRequiredService<ISqlSugarClient>();
            _clients.Push(client);
            return _clients.Peek();
        }
    }


    public override object GetCurrentClient()
    {
        var current = _clients.Count > 0 ? _clients.Peek() : null;
        if (current == null)
        {
            return CreateClient();
        }

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
        switch (propagation)
        {
            case Propagation.Required:
            {
                var client = _clients.Count > 0
                    ? this.GetCurrentClient<ISqlSugarClient>()
                    : CreateClient();

                return new RequiredTransactionScope(client,
                    isolationLevel,
                    client.Ado.IsAnyTran(),
                    _onAfterScope
                );
            }
            case Propagation.RequiresNew:
            {
                var client = CreateClient();
                return new RequiresNewTransactionScope(client, isolationLevel, _onAfterScope);
            }
            case Propagation.Mandatory:
            {
                var client = _propagations.Count > 0
                    ? this.GetCurrentClient<ISqlSugarClient>()
                    : throw new InvalidOperationException("No existing transaction found for transaction marked with propagation 'Mandatory'.");

                return new MandatoryTransactionScope(client,
                    isolationLevel, _onAfterScope
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
                    return new RequiredTransactionScope(client, isolationLevel, false, _onAfterScope);
                }

                // 有外层事务：使用 savepoint 实现嵌套事务
                return new NestedTransactionScope(client, isolationLevel, _onAfterScope);
            }
            case Propagation.Never:
            {
                // 如果当前存在任何 transaction propagation，则不允许在事务中运行
                if (_propagations.Count > 0)
                {
                    throw new InvalidOperationException("Existing transaction found for transaction marked with propagation 'Never'.");
                }

                // 无 ambient：按非事务方式执行（Begin/Commit/Rollback 都为 no-op）
                var client = _clients.Count > 0 ? this.GetCurrentClient<ISqlSugarClient>() : CreateClient();
                return new NoTransactionScope(client, isolationLevel, _onAfterScope);
            }

            default:
                throw new NotSupportedException($"Propagation {propagation} not supported.");
        }
    }
}