using Kurisu.AspNetCore.Abstractions.DataAccess.Core;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Extensions;

public static class DatasourceManagerExtensions
{
    public static TClient GetCurrentClient<TClient>(this IDatasourceManager mgr) where TClient : class
    {
        return mgr.GetCurrentClient() as TClient;
    }
}