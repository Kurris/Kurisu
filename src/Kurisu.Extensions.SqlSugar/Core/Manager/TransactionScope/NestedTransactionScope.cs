using System.Data;
using SqlSugar;
using SqlSugarDbType = SqlSugar.DbType;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

internal class NestedTransactionScope : AbstractTransactionScope
{
    private readonly ISqlSugarClient _client;
    private readonly IsolationLevel? _isolationLevel;
    private readonly Action _afterScope;
    private readonly string _savepointName;
    private bool _isSavepointCreated;
    private readonly bool _hasTransaction;

    public NestedTransactionScope(ISqlSugarClient client, IsolationLevel? isolationLevel, Action afterScope)
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
        await _client.CreateSavepointAsync(_savepointName);
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
            await _client.ReleaseSavepointAsync(_savepointName);
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
        await _client.RollbackToSavepointAsync(_savepointName);
    }

    public override void Dispose()
    {
        _afterScope?.Invoke();
    }
}

internal static class SavePointExtensions
{
    private static SqlSugarDbType GetDbType(ISqlSugarClient client)
    {
        return (client as SqlSugarClient).Context.CurrentConnectionConfig.DbType;
    }

    public static async Task CreateSavepointAsync(this ISqlSugarClient client, string savepointName)
    {
        var dbType = GetDbType(client);

        if (dbType == SqlSugarDbType.SqlServer)
        {
            // SQL Server uses SAVE TRANSACTION
            await client.Ado.ExecuteCommandAsync($"SAVE TRANSACTION {savepointName};");
            return;
        }

        // MySQL/Postgres and others support SAVEPOINT
        await client.Ado.ExecuteCommandAsync($"SAVEPOINT {savepointName};");
    }

    public static async Task ReleaseSavepointAsync(this ISqlSugarClient client, string savepointName)
    {
        var dbType = GetDbType(client);

        if (dbType == SqlSugarDbType.SqlServer)
        {
            // SQL Server does not support RELEASE SAVEPOINT and doesn't need it
            return;
        }

        try
        {
            await client.Ado.ExecuteCommandAsync($"RELEASE SAVEPOINT {savepointName};");
        }
        catch
        {
            // ignore if not supported by driver
        }
    }

    public static async Task RollbackToSavepointAsync(this ISqlSugarClient client, string savepointName)
    {
        var dbType = GetDbType(client);

        if (dbType == SqlSugarDbType.SqlServer)
        {
            // SQL Server rolls back to a named savepoint using ROLLBACK TRANSACTION name
            await client.Ado.ExecuteCommandAsync($"ROLLBACK TRANSACTION {savepointName};");
            return;
        }

        await client.Ado.ExecuteCommandAsync($"ROLLBACK TO SAVEPOINT {savepointName};");
    }
}