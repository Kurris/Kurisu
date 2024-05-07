namespace Kurisu.RemoteCall.Proxy.Abstractions;

/// <summary>
/// 同步代理接口
/// </summary>
internal interface IInterceptor
{
    /// <summary>
    /// 方法触发
    /// </summary>
    /// <param name="invocation"></param>
    void Intercept(IProxyInvocation invocation);
}