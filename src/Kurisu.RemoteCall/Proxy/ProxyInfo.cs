using System.Reflection;
using Kurisu.RemoteCall.Proxy.Abstractions;

namespace Kurisu.RemoteCall.Proxy;

/// <summary>
/// 代理信息
/// </summary>
internal class ProxyInfo : IProxyInvocation
{
    /// <inheritdoc />
    public IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// 接口类型
    /// </summary>
    public Type InterfaceType { get; set; }

    /// <summary>
    /// 执行方法
    /// </summary>
    public MethodInfo Method { get; set; }

    /// <summary>
    /// 方法参数
    /// </summary>
    public object[] Parameters { get; set; }

    /// <summary>
    /// 方法返回值
    /// </summary>
    public object ReturnValue { get; set; }
}