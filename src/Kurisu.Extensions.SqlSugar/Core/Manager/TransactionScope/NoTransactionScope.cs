using System.Data;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

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