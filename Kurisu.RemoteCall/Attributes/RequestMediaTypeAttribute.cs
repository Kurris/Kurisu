namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求参数 media type
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequestMediaTypeAttribute : Attribute
{
    private readonly string _contentType;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="contentType"></param>
    public RequestMediaTypeAttribute(string contentType)
    {
        _contentType = contentType;
    }

    /// <summary>
    /// header content-type
    /// </summary>
    public string ContentType => _contentType;
}