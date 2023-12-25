namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// like aspnetcore
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class HttpMethodAttribute : Attribute
{
    public abstract HttpMethodEnumType HttpMethod { get; set; }

    public abstract string Template { get; }
}

public class GetAttribute : HttpMethodAttribute
{
    public GetAttribute()
    {
    }

    public GetAttribute(string template)
    {
        Template = template;
    }

    public override HttpMethodEnumType HttpMethod { get; set; } = HttpMethodEnumType.Get;
    public override string Template { get; }
}

public class PutAttribute : HttpMethodAttribute
{
    public PutAttribute()
    {
    }

    public PutAttribute(string template)
    {
        Template = template;
    }

    public override HttpMethodEnumType HttpMethod { get; set; } = HttpMethodEnumType.Put;
    public override string Template { get; }
}

public class PostAttribute : HttpMethodAttribute
{
    public PostAttribute()
    {
    }

    public PostAttribute(string template)
    {
        Template = template;
    }

    public override HttpMethodEnumType HttpMethod { get; set; } = HttpMethodEnumType.Post;
    public override string Template { get; }
}

public class DeleteAttribute : HttpMethodAttribute
{
    public DeleteAttribute()
    {
    }

    public DeleteAttribute(string template)
    {
        Template = template;
    }

    public override HttpMethodEnumType HttpMethod { get; set; } = HttpMethodEnumType.Delete;
    public override string Template { get; }
}

public class PatchAttribute : HttpMethodAttribute
{
    public PatchAttribute()
    {
    }

    public PatchAttribute(string template)
    {
        Template = template;
    }

    public override HttpMethodEnumType HttpMethod { get; set; } = HttpMethodEnumType.Patch;
    public override string Template { get; }
}