using Kurisu.Core.Proxy.Abstractions;

namespace Kurisu.Core.Proxy.Internal;

/// <summary>
/// 异步拦截器扩展
/// </summary>
public static class AsyncInterceptorExtensions
{
    /// <summary>
    /// ToInterceptor
    /// </summary>
    /// <param name="interceptor"></param>
    /// <returns></returns>
    public static IInterceptor ToInterceptor(this IAsyncInterceptor interceptor)
    {
        return new AsyncDeterminationInterceptor(interceptor);
    }

    /// <summary>
    /// ToInterceptor
    /// </summary>
    /// <param name="interceptors"></param>
    /// <returns></returns>
    public static IInterceptor[] ToInterceptors(this IEnumerable<IAsyncInterceptor> interceptors)
    {
        return interceptors.Select(ToInterceptor).ToArray();
    }
}