using System;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求参数 media type
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequestMediaTypeAttribute : Attribute
{
    private readonly string _type;

    public RequestMediaTypeAttribute(string type)
    {
        _type = type;
    }

    public string Type => _type;
}