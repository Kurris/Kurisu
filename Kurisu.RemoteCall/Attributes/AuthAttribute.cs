

using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求鉴权
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public class AuthAttribute : Attribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public AuthAttribute()
    {

    }

    /// <summary>
    /// header-name
    /// </summary>
    public string HeaderName { get; set; } = "Authorization";

    /// <summary>
    /// <see cref="IAsyncAuthTokenHandler"/>
    /// </summary>
    public Type TokenHandler { get; set; }
}
