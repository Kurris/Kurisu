namespace Kurisu.Proxy.Abstractions;

public interface IInterceptor
{
    void Intercept(IProxyInfo invocation);
}