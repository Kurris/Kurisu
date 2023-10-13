namespace Kurisu.Proxy.Abstractions;

/// <summary>
/// 同步代理接口
/// </summary>
public interface IInterceptor
{
    void Intercept(IProxyInvocation invocation);
}