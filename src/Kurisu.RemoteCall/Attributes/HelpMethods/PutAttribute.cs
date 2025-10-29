namespace Kurisu.RemoteCall.Attributes.HelpMethods;

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

    /// <summary>
    /// 请求方法
    /// </summary>
    public override HttpMethod HttpMethod => HttpMethod.Put;
}