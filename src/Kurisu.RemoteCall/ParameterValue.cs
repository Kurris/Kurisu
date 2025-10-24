using System.Reflection;
using Kurisu.RemoteCall.Attributes;

namespace Kurisu.RemoteCall;

/// <summary>
/// 参数值
/// </summary>
public class ParameterValue
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="p"></param>
    /// <param name="v"></param>
    public ParameterValue(ParameterInfo p, object v)
    {
        Parameter = p;
        Value = v;

        RouteAttribute = Parameter.GetCustomAttribute<RequestRouteAttribute>();
        QueryAttribute = Parameter.GetCustomAttribute<RequestQueryAttribute>();
    }

    /// <summary>
    /// 属性
    /// </summary>
    public ParameterInfo Parameter { get; set; }

    /// <summary>
    /// 值
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// 路由参数特性
    /// </summary>
    public RequestRouteAttribute RouteAttribute { get; }

    /// <summary>
    /// 查询参数特性
    /// </summary>
    public RequestQueryAttribute QueryAttribute { get; set; }
}