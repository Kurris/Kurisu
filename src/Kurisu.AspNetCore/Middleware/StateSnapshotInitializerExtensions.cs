using Microsoft.AspNetCore.Builder;

namespace Kurisu.AspNetCore.Middleware;

public static class StateSnapshotInitializerExtensions
{
    /// <summary>
    /// Add the StateSnapshotInitializerMiddleware to the pipeline. This ensures all registered IStateSnapshotManager{T} are initialized/removed per request.
    /// </summary>
    public static IApplicationBuilder UseStateSnapshotInitializer(this IApplicationBuilder app)
    {
        return app.UseMiddleware<StateSnapshotInitializerMiddleware>();
    }
}

