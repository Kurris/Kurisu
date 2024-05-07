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
    public abstract HttpMethodEnumType HttpMethod { get; set; }

    /// <summary>
    /// 请求route template
    /// </summary>
    public abstract string Template { get; }
}

/// <summary>
/// get
/// </summary>
public class GetAttribute : HttpMethodAttribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public GetAttribute()
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="template">请求route template</param>
    public GetAttribute(string template)
    {
        Template = template;
    }

    /// <summary>
    /// 请求方法
    /// </summary>
    public override HttpMethodEnumType HttpMethod { get; set; } = HttpMethodEnumType.Get;
    
    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }
}

/// <summary>
/// put
/// </summary>
public class PutAttribute : HttpMethodAttribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public PutAttribute()
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="template"></param>
    public PutAttribute(string template)
    {
        Template = template;
    }

    /// <summary>
    /// 请求方法
    /// </summary>
    public override HttpMethodEnumType HttpMethod { get; set; } = HttpMethodEnumType.Put;
    
    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }
}

/// <summary>
/// post
/// </summary>
public class PostAttribute : HttpMethodAttribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public PostAttribute()
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="template"></param>
    public PostAttribute(string template)
    {
        Template = template;
    }

    /// <summary>
    /// 请求方法
    /// </summary>
    public override HttpMethodEnumType HttpMethod { get; set; } = HttpMethodEnumType.Post;
    
    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }
}

/// <summary>
/// delete
/// </summary>
public class DeleteAttribute : HttpMethodAttribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public DeleteAttribute()
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="template"></param>
    public DeleteAttribute(string template)
    {
        Template = template;
    }

    /// <summary>
    /// 请求方法
    /// </summary>
    public override HttpMethodEnumType HttpMethod { get; set; } = HttpMethodEnumType.Delete;
    
    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }
}

/// <summary>
/// patch
/// </summary>
public class PatchAttribute : HttpMethodAttribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public PatchAttribute()
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="template"></param>
    public PatchAttribute(string template)
    {
        Template = template;
    }

    /// <summary>
    /// 请求方法
    /// </summary>
    public override HttpMethodEnumType HttpMethod { get; set; } = HttpMethodEnumType.Patch;
    
    /// <summary>
    /// 请求route template
    /// </summary>
    public override string Template { get; }
}