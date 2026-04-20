using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;


public abstract class AbstractDatasourceManager<TClient> : IDatasourceManager<TClient>
{
    public abstract TClient GetCurrentClient();

    public abstract TClientDefined GetCurrentClient<TClientDefined>();

    public abstract ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null);
}