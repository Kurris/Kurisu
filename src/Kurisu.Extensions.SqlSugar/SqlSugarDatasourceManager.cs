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
    private readonly Action _onDispose;

    public int ClientCount => _clients.Count;

    public SqlSugarDatasourceManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _onDispose = () =>
        {
            _propagations.Pop();
            
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

    public ITransactionScope CreateTransScopeInternal(Propagation propagation, IsolationLevel? isolationLevel = null)
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
                    _onDispose
                );
            }
            case Propagation.RequiresNew:
            {
                var client = CreateClient();
                return new RequiresNewTransactionScope(client, isolationLevel, _onDispose);
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
        private readonly Action _onDispose;

        public RequiredTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, bool hasTransaction = false, Action onDispose = null)
        {
            _client = client;
            _isolationLevel = isolationLevel;
            _hasTransaction = hasTransaction;
            _onDispose = onDispose;
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
            if (!_hasTransaction)
            {
                _onDispose?.Invoke();
            }
        }
    }

    public class RequiresNewTransactionScope : RequiredTransactionScope
    {
        private readonly Action _toDispose;

        public RequiresNewTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action toDispose)
            : base(client, isolationLevel)
        {
            _toDispose = toDispose;
        }

        public override void Dispose()
        {
            _toDispose?.Invoke();
        }
    }
}