namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 参数指定为url query
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class RequestQueryAttribute : Attribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public RequestQueryAttribute()
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="name"></param>
    public RequestQueryAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; }
}