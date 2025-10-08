using System.Data;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar;

public class SqlSugarDatasourceManager : IDatasourceManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<ISqlSugarClient> _clients = new();

    public SqlSugarDatasourceManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _clients.Push(_serviceProvider.GetRequiredService<ISqlSugarClient>());
    }

    public object CurrentDbClient => (_clients.Count > 0 ? _clients.Peek() : null);

    public void NewClient()
    {
        var client = _serviceProvider.GetRequiredService<ISqlSugarClient>();
        _clients.Push(client);
    }

    public void NewClient(string name)
    {
        var dbConnectionManager = _serviceProvider.GetRequiredService<IDbConnectionManager>();
        dbConnectionManager.Switch(name);
        var client = _serviceProvider.GetRequiredService<ISqlSugarClient>();
        dbConnectionManager.Switch();
        _clients.Push(client);
    }


    public void Dispose()
    {
        foreach (var client in _clients)
        {
            client.Dispose();
        }
    }

    public ITransactionScope CreateScope(Propagation propagation, IsolationLevel? isolationLevel = null)
    {
        switch (propagation)
        {
            case Propagation.Required:
                return new RequiredTransactionScope((ISqlSugarClient)CurrentDbClient, isolationLevel, _clients.Count > 1, () =>
                {
                    // do nothing
                });
            case Propagation.RequiresNew:
                var newClient = _serviceProvider.GetRequiredService<ISqlSugarClient>();
                _clients.Push(newClient);
                return new RequiredTransactionScope(newClient, isolationLevel, false, () =>
                {
                    var poppedClient = _clients.Pop();
                    poppedClient.Dispose();
                });

            default:
                throw new NotSupportedException($"Propagation {propagation} not supported.");
        }
    }

    public class RequiredTransactionScope : ITransactionScope
    {
        private readonly ISqlSugarClient _client;
        private readonly IsolationLevel? _isolationLevel;
        private readonly bool _hasTransaction;
        private readonly Action? _toDispose;

        public RequiredTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, bool hasTransaction = false, Action toDispose = null)
        {
            _client = client;
            _isolationLevel = isolationLevel;
            _hasTransaction = hasTransaction;
            _toDispose = toDispose;
        }

        public async Task BeginAsync()
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

        public async Task CommitAsync()
        {
            if (_hasTransaction)
            {
                return;
            }

            await _client.Ado.CommitTranAsync();
        }

        public async Task RollbackAsync()
        {
            if (_hasTransaction)
            {
                return;
            }

            await _client.Ado.RollbackTranAsync();
        }

        public void Dispose()
        {
            _toDispose?.Invoke();
        }
    }
}