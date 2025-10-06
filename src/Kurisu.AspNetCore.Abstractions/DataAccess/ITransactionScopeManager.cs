using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess;

public interface ITransactionScopeManager : IDisposable
{
    IDbTransaction Transaction { get; }

    Task CommitAsync();

    Task RollbackAsync();
}