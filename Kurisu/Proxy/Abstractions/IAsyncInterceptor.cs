namespace Kurisu.Proxy.Abstractions;


/// <summary>
/// 异步同步代理接口
/// </summary>
public interface IAsyncInterceptor
{
    void InterceptSynchronous(IProxyInvocation invocation);

    void InterceptAsynchronous(IProxyInvocation invocation);

    void InterceptAsynchronous<TResult>(IProxyInvocation invocation);
}
