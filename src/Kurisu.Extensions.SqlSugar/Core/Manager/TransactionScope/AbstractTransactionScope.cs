using Kurisu.AspNetCore.Abstractions.DataAccess.Core;

namespace Kurisu.Extensions.SqlSugar.Core.Manager.TransactionScope;

internal abstract class AbstractTransactionScope : ITransactionScope
{
    public abstract Task BeginAsync();

    public abstract Task CommitAsync();

    public abstract Task RollbackAsync();

    public abstract void Dispose();
}