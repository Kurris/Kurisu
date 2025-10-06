using System.Data;

using Kurisu.AspNetCore.Abstractions.DataAccess;

public class NullTransactionScope : ITransactionScopeManager
{
    public static readonly NullTransactionScope Instance = new NullTransactionScope();
    private NullTransactionScope() { }

    public void Dispose() { }

    public IDbTransaction Transaction => null;

    public Task CommitAsync() => Task.CompletedTask;

    public Task RollbackAsync() => Task.CompletedTask;
}