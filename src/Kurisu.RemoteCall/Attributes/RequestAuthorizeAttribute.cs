using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Default;
using Kurisu.RemoteCall.Utils;
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
    public RequestAuthorizeAttribute() : this(typeof(DefaultRemoteCallAuthTokenHandler))
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    public RequestAuthorizeAttribute(Type handler)
    {
        if (handler == null) throw new ArgumentException(nameof(handler));
        if (!handler.IsInheritedFrom<IRemoteCallAuthTokenHandler>()) throw new ArgumentException(nameof(handler) + " is not inherited from IRemoteCallAuthTokenHandler");
        Handler = handler;
    }

    /// <summary>
    /// header-name
    /// </summary>
    public string HeaderName { get; set; } = HeaderNames.Authorization;

    /// <summary>
    /// <see cref="IRemoteCallAuthTokenHandler"/>
    /// </summary>
    public Type Handler { get; }
}