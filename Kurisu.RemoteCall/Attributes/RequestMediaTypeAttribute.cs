namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求参数 media type
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequestMediaTypeAttribute : Attribute
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="contentType"></param>
    public RequestMediaTypeAttribute(string contentType)
    {
        ContentType = contentType;
        if (contentType == "application/x-www-form-urlencoded")
        {
            IsUrlEncoded = true;
        }
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="contentType"></param>
    /// <param name="isUrlEncoded"></param>
    public RequestMediaTypeAttribute(string contentType, bool isUrlEncoded)
    {
        IsUrlEncoded = isUrlEncoded;
        ContentType = contentType;
    }

    /// <summary>
    /// header content-type
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// IsUrlEncoded
    /// </summary>
    public bool IsUrlEncoded { get; }
}