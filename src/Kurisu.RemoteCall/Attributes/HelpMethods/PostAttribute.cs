namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// post
/// </summary>
public sealed class PostAttribute : BaseHttpMethodAttribute
{
    /// <summary>
    /// post
    /// </summary>
    public PostAttribute() : this(string.Empty)
    {
    }

    /// <summary>
    ///  post
    /// </summary>
    /// <param name="template"></param>
    /// <param name="contentType"></param>
    /// <param name="isUrlencoded"></param>
    public PostAttribute(string template, string contentType = "application/json", bool isUrlencoded = false)
    {
        Template = template;
        ContentType = contentType;
        IsUrlencoded = isUrlencoded;

        if (ContentType == "application/x-www-form-urlencoded")
        {
            IsUrlencoded = true;
        }
    }


    /// <summary>
    /// ContentType
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// 是否是Urlencoded
    /// </summary>
    public bool IsUrlencoded { get; }

    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }

    public override HttpMethod HttpMethod => HttpMethod.Post;
}