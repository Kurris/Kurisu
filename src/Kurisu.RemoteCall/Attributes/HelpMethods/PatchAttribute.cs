namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// patch
/// </summary>
public sealed class PatchAttribute : BaseHttpMethodAttribute
{
    /// <summary>
    /// patch
    /// </summary>
    public PatchAttribute() : this(string.Empty)
    {
    }

    /// <summary>
    /// patch
    /// </summary>
    /// <param name="template"></param>
    public PatchAttribute(string template)
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
    public override HttpMethod HttpMethod => HttpMethod.Patch;
}