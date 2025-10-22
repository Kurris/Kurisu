using System.Reflection;
using Kurisu.RemoteCall.Default;
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
    public ParameterInfo[] ParameterInfos { get; set; }

    /// <summary>
    ///  参数值
    /// </summary>
    public object[] ParameterValues { get; set; }

    /// <summary>
    ///  包装后的参数值
    /// </summary>
    public List<ParameterValue> WrapParameterValues { get; set; }

    /// <summary>
    /// 方法返回值
    /// </summary>
    public object ReturnValue { get; set; }

    public RemoteClient RemoteClient { get; set; }
}