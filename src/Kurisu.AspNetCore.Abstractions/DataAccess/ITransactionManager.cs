using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess;

public interface ITransactionManager
{
    ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null);
}

/// <summary>
/// 数据库操作作用域
/// </summary>
public interface IDataBaseOperateScope
{
    
}


public interface ITransactionScope : IDisposable
{
    Task BeginAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

public abstract class AbstractTransactionScope : ITransactionScope
{
    public TransactionMarkType MarkType { get; set; }

    public abstract Task BeginAsync();

    public abstract Task CommitAsync();

    public abstract Task RollbackAsync();

    public abstract void Dispose();
}

public enum TransactionMarkType
{
    Unknown,
    Rollback,
    Committed
}