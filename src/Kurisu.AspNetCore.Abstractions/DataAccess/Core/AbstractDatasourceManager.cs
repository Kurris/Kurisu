using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;


public interface IDatasourceManager : ITransactionManager
{
    object GetCurrentClient();
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

    public abstract TClient CreateClient();

    public abstract TClient CreateClient(string name);

    public abstract ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null);
}