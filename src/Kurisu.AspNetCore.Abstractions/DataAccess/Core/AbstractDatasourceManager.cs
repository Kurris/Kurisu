using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;


public abstract class AbstractDatasourceManager<TClient> : IDatasourceManager<TClient>
{
    //<inheritdoc/>
    public abstract TClient GetCurrentClient();

    //<inheritdoc/>
    public abstract IDisposable CreateScope(string name);

    //<inheritdoc/>
    public abstract ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null);

    public abstract TClientDefined GetCurrentClient<TClientDefined>();
}