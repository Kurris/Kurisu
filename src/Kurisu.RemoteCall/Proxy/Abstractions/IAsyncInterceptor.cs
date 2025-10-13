using System.Collections.Concurrent;

namespace Kurisu.RemoteCall.Proxy.Abstractions;

/// <summary>
/// 代理拦截接口
/// </summary>
internal interface IAsyncInterceptor : IInterceptor
{
    /// <summary>
    /// 异步方法触发
    /// </summary>
    /// <param name="invocation"></param>
    void InterceptAsync(IProxyInvocation invocation);

    /// <summary>
    /// 异步方法触发
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="invocation"></param>
    void InterceptAsync<TResult>(IProxyInvocation invocation);
}

internal static class AsyncInterceptorExtensions
{
    private static readonly ConcurrentDictionary<string, AsyncDeterminationInterceptor> Clients = new();

    public static IInterceptor ToInterceptor(this BaseRemoteCallClient interceptor)
    {
        return Clients.GetOrAdd(interceptor.RequestType, new AsyncDeterminationInterceptor(interceptor));
    }
}