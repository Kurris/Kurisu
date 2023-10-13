using System.Collections.Generic;
using System.Linq;
using Kurisu.Proxy.Abstractions;

namespace Kurisu.Proxy.Internal;

internal static class AsyncInterceptorExtensions
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