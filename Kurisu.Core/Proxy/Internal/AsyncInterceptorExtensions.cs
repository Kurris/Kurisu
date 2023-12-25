using Kurisu.Core.Proxy.Abstractions;

namespace Kurisu.Core.Proxy.Internal;

public static class AsyncInterceptorExtensions
{
    public static IInterceptor ToInterceptor(this IAsyncInterceptor interceptor)
    {
        return new AsyncDeterminationInterceptor(interceptor);
    }

    public static IInterceptor[] ToInterceptors(this IEnumerable<IAsyncInterceptor> interceptors)
    {
        return interceptors.Select(ToInterceptor).ToArray();
    }
}