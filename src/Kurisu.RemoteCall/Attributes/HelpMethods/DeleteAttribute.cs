namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// delete
/// </summary>
public sealed class DeleteAttribute : BaseHttpMethodAttribute
{
    /// <summary>
    /// delete
    /// </summary>
    public DeleteAttribute() : this(string.Empty)
    {
    }

    /// <summary>
    /// delete
    /// </summary>
    /// <param name="template"></param>
    public DeleteAttribute(string template)
    {
        Template = template;
        HttpMethodType = HttpMethodType.Delete;
    }

    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }

    public override HttpMethod HttpMethod => HttpMethod.Delete;
}