namespace Kurisu.RemoteCall.Attributes.HelpMethods;

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
    /// <param name="asUrlencodedFormat"></param>
    public PostAttribute(string template, string contentType = "application/json", bool asUrlencodedFormat = false)
    {
        Template = template;
        ContentType = contentType;
        AsUrlencodedFormat = asUrlencodedFormat;

        // 如果显式指定 contentType 为 form-urlencoded，则强制将 AsUrlencodedFormat 置为 true
        if (string.Equals(ContentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            AsUrlencodedFormat = true;
        }

        // NOTE: 不再自动把 ContentType 改为 form-urlencoded 当 asUrlencodedFormat 为 true；
        // asUrlencodedFormat 仅表示参数转成 key=val&... 格式，而不代表真实表单提交
    }


    /// <summary>
    /// ContentType
    /// </summary>
    public string ContentType { get; private set; }

    /// <summary>
    /// 表示参数是否以 <c>arg1=val1&amp;arg2=val2</c> 的格式编码（并非真实表单提交）
    /// </summary>
    public bool AsUrlencodedFormat { get; private set; }

    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public override HttpMethod HttpMethod => HttpMethod.Post;
}