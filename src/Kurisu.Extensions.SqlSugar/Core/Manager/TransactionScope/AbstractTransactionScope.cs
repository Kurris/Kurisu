using Kurisu.AspNetCore.Abstractions.DataAccess.Core;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

public enum TransactionMarkType
{
    Unknown,
    Rollback,
    Committed
}

public abstract class AbstractTransactionScope : ITransactionScope
{
    public TransactionMarkType MarkType { get; set; }

    public abstract Task BeginAsync();

    public abstract Task CommitAsync();

    public abstract Task RollbackAsync();

    public abstract void Dispose();
}