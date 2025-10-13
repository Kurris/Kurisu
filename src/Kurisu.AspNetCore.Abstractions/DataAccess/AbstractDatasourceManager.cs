using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess;

public abstract class AbstractDatasourceManager<TClient> : IDatasourceManager
{
    public abstract object GetCurrentClient();

    protected TCaseClient GetCurrentClient<TCaseClient>()
    {
        return (TCaseClient)GetCurrentClient();
    }

    public abstract ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null);

    public abstract TClient CreateClient();

    public abstract TClient CreateClient(string name);
}