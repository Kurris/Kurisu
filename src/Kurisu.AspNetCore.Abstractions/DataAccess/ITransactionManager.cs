using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess;

public interface ITransactionManager
{
    ITransactionScope CreateScope(Propagation propagation, IsolationLevel? isolationLevel = null);
}

public interface ITransactionScope : IDisposable
{
    Task BeginAsync();
    Task CommitAsync();
    Task RollbackAsync();
}