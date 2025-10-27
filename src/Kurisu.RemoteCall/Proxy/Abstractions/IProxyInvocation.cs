using System.Reflection;
using Kurisu.RemoteCall.Default;

namespace Kurisu.RemoteCall.Proxy.Abstractions;

/// <summary>
/// 代理调用信息
/// </summary>
public interface IProxyInvocation
{
    /// <summary>
    /// service provider
    /// </summary>
    public IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// 接口类型
    /// </summary>
    public Type InterfaceType { get; set; }

    /// <summary>
    /// 代理方法
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
    /// 包装后的参数值
    /// </summary>
    public List<ParameterValue> WrapParameterValues { get; set; }

    /// <summary>
    /// 返回值
    /// </summary>
    object ReturnValue { get; set; }

    /// <summary>
    /// 客户端
    /// </summary>
    public RemoteClient RemoteClient { get; set; }
}