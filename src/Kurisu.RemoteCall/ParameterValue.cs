using System.Reflection;

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
    }

    /// <summary>
    /// 属性
    /// </summary>
    public ParameterInfo Parameter { get; set; }

    /// <summary>
    /// 值
    /// </summary>
    public object Value { get; set; }
}