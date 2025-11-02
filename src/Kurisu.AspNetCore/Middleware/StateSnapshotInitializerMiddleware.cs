using System.Linq;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Middleware;

/// <summary>
/// Middleware that initializes and removes all registered IStateSnapshotManager{TState} instances per request.
/// It discovers closed generic service registrations (types) and for each request resolves the manager instance
/// from the request service provider, calls Initialize() before the next middleware, and Remove() in finally.
/// </summary>
public class StateSnapshotInitializerMiddleware
{
    private readonly RequestDelegate _next;

    public StateSnapshotInitializerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Resolve all lifecycle-capable managers from the current request scope and initialize them.
        var lifecycles = context.RequestServices.GetServices<IStateSnapshotLifecycle>().ToList();

        foreach (var lifecycle in lifecycles)
        {
            try
            {
                lifecycle.Initialize();
            }
            catch
            {
                // best-effort
            }
        }

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            foreach (var lifecycle in lifecycles)
            {
                try
                {
                    lifecycle.Remove();
                }
                catch
                {
                    // swallow
                }
            }
        }
    }
}