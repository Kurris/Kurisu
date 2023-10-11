namespace Kurisu.Proxy.Abstractions;


public interface IAsyncInterceptor
{
    void InterceptSynchronous(IProxyInfo invocation);

    void InterceptAsynchronous(IProxyInfo invocation);

    void InterceptAsynchronous<TResult>(IProxyInfo invocation);
}
