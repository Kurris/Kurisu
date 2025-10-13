using Kurisu.RemoteCall.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求鉴权
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class RequestAuthorizeAttribute : Attribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public RequestAuthorizeAttribute()
    {
    }

    /// <summary>
    /// header-name
    /// </summary>
    public string HeaderName { get; set; } = HeaderNames.Authorization;

    /// <summary>
    /// <see cref="IRemoteCallAuthTokenHandler"/>
    /// </summary>
    public Type Handler { get; set; }
}