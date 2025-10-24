// ReSharper disable once CheckNamespace

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// like aspnetcore
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class BaseHttpMethodAttribute : Attribute
{
    /// <summary>
    /// 请求route template
    /// </summary>
    public abstract string Template { get; }

    /// <summary>
    /// 实际的HttpMethod
    /// </summary>
    public abstract HttpMethod HttpMethod { get; }
}