using System.Reflection;

namespace Kurisu.Proxy.Abstractions;

/// <summary>
/// 代理调用信息
/// </summary>
public interface IProxyInvocation
{
    /// <summary>
    /// 代理对象
    /// </summary>
    public object Target { get; set; }

    /// <summary>
    /// 代理方法
    /// </summary>
    public MethodInfo Method { get; set; }

    /// <summary>
    /// 代理参数
    /// </summary>
    public object[] Parameters { get; set; }

    /// <summary>
    /// 返回值
    /// </summary>
    object ReturnValue { get; set; }

    /// <summary>
    /// 方法执行
    /// </summary>
    void Proceed();
}
