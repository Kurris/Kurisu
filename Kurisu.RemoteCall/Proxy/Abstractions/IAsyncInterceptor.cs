namespace Kurisu.RemoteCall.Proxy.Abstractions;

/// <summary>
/// 异步同步代理接口
/// </summary>
internal interface IAsyncInterceptor
{
    /// <summary>
    /// 同步方法触发
    /// </summary>
    /// <param name="invocation"></param>
    void InterceptSynchronous(IProxyInvocation invocation);

    /// <summary>
    /// 异步方法触发
    /// </summary>
    /// <param name="invocation"></param>
    void InterceptAsynchronous(IProxyInvocation invocation);

    /// <summary>
    /// 异步方法触发
    /// </summary>
    /// <typeparam name="TResult">返回值类型</typeparam>
    /// <param name="invocation"></param>
    void InterceptAsynchronous<TResult>(IProxyInvocation invocation);
}
