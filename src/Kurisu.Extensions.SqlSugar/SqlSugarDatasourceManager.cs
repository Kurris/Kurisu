using System.Data;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar;

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
        dbConnectionManager.Switch(name);
        var client = _serviceProvider.GetRequiredService<ISqlSugarClient>();
        dbConnectionManager.Switch();
        _clients.Push(client);
        return client;
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
                    ? GetCurrentClient<ISqlSugarClient>()
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
                    ? GetCurrentClient<ISqlSugarClient>()
                    : throw new InvalidOperationException("No existing transaction found for transaction marked with propagation 'Mandatory'.");

                return new MandatoryTransactionScope(client,
                    isolationLevel, _onAfterScope
                );
            }
            case Propagation.Nested:
            {
                // 如果没有 ambient transaction，则与 Required 行为一致（新建事务）
                var client = _clients.Count > 0 ? GetCurrentClient<ISqlSugarClient>() : CreateClient();
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
                var client = _clients.Count > 0 ? GetCurrentClient<ISqlSugarClient>() : CreateClient();
                return new NoTransactionScope(client, isolationLevel, _onAfterScope);
            }

            default:
                throw new NotSupportedException($"Propagation {propagation} not supported.");
        }
    }


    public class RequiredTransactionScope : AbstractTransactionScope
    {
        private readonly ISqlSugarClient _client;
        private readonly IsolationLevel? _isolationLevel;
        private readonly bool _hasTransaction;
        private readonly Action<bool> _afterScope;

        public RequiredTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, bool hasTransaction = false, Action<bool> afterScope = null)
        {
            _client = client;
            _isolationLevel = isolationLevel;
            _hasTransaction = hasTransaction;
            _afterScope = afterScope;
        }

        public override async Task BeginAsync()
        {
            if (_hasTransaction)
            {
                return;
            }

            if (_isolationLevel.HasValue)
            {
                await _client.Ado.BeginTranAsync(_isolationLevel.Value);
            }
            else
            {
                await _client.Ado.BeginTranAsync();
            }
        }

        public override async Task CommitAsync()
        {
            if (_hasTransaction)
            {
                return;
            }

            await _client.Ado.CommitTranAsync();
        }

        public override async Task RollbackAsync()
        {
            if (_hasTransaction)
            {
                return;
            }

            await _client.Ado.RollbackTranAsync();
        }

        public override void Dispose()
        {
            _afterScope?.Invoke(!_hasTransaction);
        }
    }

    public class RequiresNewTransactionScope : RequiredTransactionScope
    {
        public RequiresNewTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action<bool> afterScope)
            : base(client, isolationLevel, false, afterScope)
        {
        }
    }

    public class MandatoryTransactionScope : RequiredTransactionScope
    {
        public MandatoryTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action<bool> afterScope)
            : base(client, isolationLevel, client.Ado.IsAnyTran(), afterScope)
        {
        }
    }

    public class NestedTransactionScope : AbstractTransactionScope
    {
        private readonly ISqlSugarClient _client;
        private readonly IsolationLevel? _isolationLevel;
        private readonly Action<bool> _afterScope;
        private readonly string _savepointName;
        private bool _isSavepointCreated;
        private readonly bool _hasTransaction;

        public NestedTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action<bool> afterScope)
        {
            _client = client;
            _isolationLevel = isolationLevel;
            _afterScope = afterScope;
            _savepointName = "SP_" + Guid.NewGuid().ToString("N");
            _isSavepointCreated = false;
            _hasTransaction = client.Ado.IsAnyTran();
        }

        public override async Task BeginAsync()
        {
            // 当存在外层事务时，创建 savepoint；若没有外层事务（不应走到这里）则开启新事务
            if (!_client.Ado.IsAnyTran())
            {
                if (_isolationLevel.HasValue)
                {
                    await _client.Ado.BeginTranAsync(_isolationLevel.Value);
                }
                else
                {
                    await _client.Ado.BeginTranAsync();
                }

                return;
            }

            // 创建 savepoint（MySQL/Postgres 风格）
            await _client.Ado.ExecuteCommandAsync($"SAVEPOINT {_savepointName};");
            _isSavepointCreated = true;
        }

        public override async Task CommitAsync()
        {
            if (!_isSavepointCreated)
            {
                // 如果没有 savepoint（可能是新开事务），直接提交
                await _client.Ado.CommitTranAsync();
                return;
            }

            // 对于 savepoint，通常不需要显式释放，部分数据库支持 RELEASE SAVEPOINT
            try
            {
                await _client.Ado.ExecuteCommandAsync($"RELEASE SAVEPOINT {_savepointName};");
            }
            catch
            {
                // 某些驱动/数据库 不支持 RELEASE SAVEPOINT，可以忽略错误
            }
        }

        public override async Task RollbackAsync()
        {
            if (!_isSavepointCreated)
            {
                // 非 savepoint 情形，回滚整个事务
                await _client.Ado.RollbackTranAsync();
                return;
            }

            // 回滚到 savepoint（只撤销内层改动）
            await _client.Ado.ExecuteCommandAsync($"ROLLBACK TO SAVEPOINT {_savepointName};");
        }

        public override void Dispose()
        {
            _afterScope?.Invoke(!_hasTransaction);
        }
    }

    public class NoTransactionScope : AbstractTransactionScope
    {
        private readonly Action<bool> _afterScope;

        public NoTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action<bool> afterScope)
        {
            // client 参数 保留以便在需要时执行 SQL；但 NoTransactionScope 本身不管理事务
            // 使用 isolationLevel 参数以避免未使用参数的分析器警告
            _ = isolationLevel;
            _afterScope = afterScope;
        }

        public override Task BeginAsync()
        {
            // Explicitly do nothing: never start a transaction
            return Task.CompletedTask;
        }

        public override Task CommitAsync()
        {
            // No transaction to commit
            return Task.CompletedTask;
        }

        public override Task RollbackAsync()
        {
            // No transaction to rollback
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            // 调用 afterScope，但不弹出 client（因为未创建新 client/事务）
            _afterScope?.Invoke(false);
        }
    }
}