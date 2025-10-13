namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 定义MediaType
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequestMediaTypeAttribute : Attribute
{
    /// <summary>
    /// init
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
    /// init
    /// </summary>
    /// <param name="contentType"></param>
    /// <param name="isUrlEncoded"></param>
    public RequestMediaTypeAttribute(string contentType, bool isUrlEncoded)
    {
        ContentType = contentType;
        IsUrlEncoded = isUrlEncoded;
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