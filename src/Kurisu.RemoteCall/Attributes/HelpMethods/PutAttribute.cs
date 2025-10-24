namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// put
/// </summary>
public sealed class PutAttribute : BaseHttpMethodAttribute
{
    /// <summary>
    /// put
    /// </summary>
    public PutAttribute() : this(string.Empty)
    {
    }

    /// <summary>
    /// put
    /// </summary>
    /// <param name="template"></param>
    public PutAttribute(string template)
    {
        Template = template;
    }

    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }

    public override HttpMethod HttpMethod => HttpMethod.Put;
}