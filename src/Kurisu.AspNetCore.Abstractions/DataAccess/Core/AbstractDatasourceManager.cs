using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

public interface IDatasourceManager : ITransactionManager
{
    object GetCurrentClient();

    IDisposable CreateScope(string name);
}

public interface ITransactionScope : IDisposable
{
    Task BeginAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

public abstract class AbstractDatasourceManager<TClient> : IDatasourceManager
{
    public abstract object GetCurrentClient();
    public abstract IDisposable CreateScope(string name);

    public abstract TClient CreateClient();

    public abstract ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null);
}