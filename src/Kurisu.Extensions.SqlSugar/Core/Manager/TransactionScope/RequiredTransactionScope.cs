using System.Data;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

internal class RequiredTransactionScope : AbstractTransactionScope
{
    private readonly ISqlSugarClient _client;
    private readonly IsolationLevel? _isolationLevel;
    private readonly bool _hasTransaction;
    private readonly Action _afterScope;
    private readonly TransactionCallbackRegistry _callbackRegistry;

    public RequiredTransactionScope(ISqlSugarClient client,
        IsolationLevel? isolationLevel,
        bool hasTransaction,
        Action afterScope,
        TransactionCallbackRegistry callbackRegistry)
    {
        _client = client;
        _isolationLevel = isolationLevel;
        _hasTransaction = hasTransaction;
        _afterScope = afterScope;
        _callbackRegistry = callbackRegistry;
    }

    public override async Task BeginAsync()
    {
        //存在事务则不创建新事务
        if (_hasTransaction)
        {
            return;
        }

        //开启事务
        if (_isolationLevel.HasValue)
        {
            await _client.Ado.BeginTranAsync(_isolationLevel.Value);
        }
        else
        {
            await _client.Ado.BeginTranAsync();
        }

        _callbackRegistry.BeginRoot();
    }

    public override async Task CommitAsync()
    {
        if (_hasTransaction)
        {
            return;
        }

        await _client.Ado.CommitTranAsync();
        await _callbackRegistry.CommitRootAsync();
    }

    public override async Task RollbackAsync()
    {
        if (_hasTransaction)
        {
            return;
        }

        await _client.Ado.RollbackTranAsync();
        _callbackRegistry.RollbackRoot();
    }

    public override void Dispose()
    {
        _afterScope?.Invoke();
    }
}