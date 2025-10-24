namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// get
/// </summary>
public sealed class GetAttribute : BaseHttpMethodAttribute
{
    /// <summary>
    /// get
    /// </summary>
    public GetAttribute() : this(string.Empty)
    {
    }

    /// <summary>
    /// get
    /// </summary>
    /// <param name="template">请求route template</param>
    public GetAttribute(string template)
    {
        Template = template;
    }

    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }

    public override HttpMethod HttpMethod => HttpMethod.Get;
}