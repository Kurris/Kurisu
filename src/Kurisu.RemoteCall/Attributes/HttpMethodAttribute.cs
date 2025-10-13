namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// like aspnetcore
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class HttpMethodAttribute : Attribute
{
    /// <summary>
    /// 请求方法
    /// </summary>
    internal HttpMethodType HttpMethod { get; set; }

    /// <summary>
    /// 请求route template
    /// </summary>
    public abstract string Template { get; }
}

/// <summary>
/// get
/// </summary>
public sealed class GetAttribute : HttpMethodAttribute
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
        HttpMethod = HttpMethodType.Get;
    }

    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }
}

/// <summary>
/// put
/// </summary>
public sealed class PutAttribute : HttpMethodAttribute
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
        HttpMethod = HttpMethodType.Put;
    }

    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }
}

/// <summary>
/// post
/// </summary>
public sealed class PostAttribute : HttpMethodAttribute
{
    /// <summary>
    /// post
    /// </summary>
    public PostAttribute() : this(string.Empty)
    {
    }

    /// <summary>
    /// post
    /// </summary>
    /// <param name="template"></param>
    public PostAttribute(string template)
    {
        Template = template;
        HttpMethod = HttpMethodType.Post;
    }

    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }
}

/// <summary>
/// delete
/// </summary>
public sealed class DeleteAttribute : HttpMethodAttribute
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
        HttpMethod = HttpMethodType.Delete;
    }

    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }
}

/// <summary>
/// patch
/// </summary>
public sealed class PatchAttribute : HttpMethodAttribute
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
        HttpMethod = HttpMethodType.Delete;
    }

    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }
}