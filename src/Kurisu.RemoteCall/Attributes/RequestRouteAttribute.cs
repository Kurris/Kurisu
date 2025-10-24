namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 参数指定为route值
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class RequestRouteAttribute : Attribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public RequestRouteAttribute()
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="name">route name</param>
    public RequestRouteAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 路由key名称
    /// </summary>
    public string Name { get; }
}