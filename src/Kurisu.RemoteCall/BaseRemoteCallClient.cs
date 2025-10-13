using Kurisu.RemoteCall.Proxy;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Kurisu.RemoteCall;

/// <summary>
/// 默认远程调用客户端基类，提供AOP拦截和远程请求处理的抽象实现。
/// </summary>
internal abstract class BaseRemoteCallClient : Aop
{
    /// <summary>
    /// 初始化 <see cref="BaseRemoteCallClient"/> 类的新实例。
    /// </summary>
    /// <param name="logger">日志记录器实例，用于记录远程调用相关日志。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="logger"/> 为 null 时抛出。</exception>
    protected BaseRemoteCallClient(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取远程调用的请求类型标识。
    /// </summary>
    public abstract string RequestType { get; }

    /// <summary>
    /// 获取日志记录器实例。
    /// </summary>
    protected ILogger Logger { get; }

    /// <inheritdoc/>
    /// <summary>
    /// 处理无返回值的异步代理调用。
    /// </summary>
    /// <param name="invocation">代理调用信息。</param>
    protected override async Task HandleAsync(IProxyInvocation invocation)
    {
        _ = await RequestAsync<object>(invocation);
    }

    /// <inheritdoc/>
    /// <summary>
    /// 处理有返回值的异步代理调用。
    /// </summary>
    /// <typeparam name="TResult">返回值类型。</typeparam>
    /// <param name="invocation">代理调用信息。</param>
    /// <returns>远程调用的返回结果。</returns>
    protected override async Task<TResult> HandleAsync<TResult>(IProxyInvocation invocation)
    {
        return await RequestAsync<TResult>(invocation);
    }

    /// <summary>
    /// 远程请求处理抽象方法，由子类实现具体的远程调用逻辑。
    /// </summary>
    /// <typeparam name="TResult">返回值类型。</typeparam>
    /// <param name="invocation">代理调用信息。</param>
    /// <returns>远程调用的返回结果。</returns>
    /// <exception cref="NotSupportedException">不支持的请求类型时抛出。</exception>
    /// <exception cref="FileLoadException">文件加载失败时抛出。</exception>
    protected abstract Task<TResult> RequestAsync<TResult>(IProxyInvocation invocation);
}