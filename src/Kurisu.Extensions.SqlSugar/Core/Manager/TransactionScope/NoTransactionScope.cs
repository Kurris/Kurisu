using System.Data;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

internal class NoTransactionScope : AbstractTransactionScope
{
    private readonly Action _afterScope;

    public NoTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action afterScope)
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
        _afterScope?.Invoke();
    }
}