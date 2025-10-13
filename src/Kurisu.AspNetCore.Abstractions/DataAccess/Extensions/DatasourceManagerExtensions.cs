using Kurisu.AspNetCore.Abstractions.DataAccess;

// ReSharper disable once CheckNamespace
namespace Kurisu.AspNetCore.Extensions;

public static class DatasourceManagerExtensions
{
    public static TClient GetCurrentClient<TClient>(this IDatasourceManager mgr) where TClient : class
    {
        return mgr.GetCurrentClient() as TClient;
    }
}