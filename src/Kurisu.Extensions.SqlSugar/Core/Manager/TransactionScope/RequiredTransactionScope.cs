using System.Data;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

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