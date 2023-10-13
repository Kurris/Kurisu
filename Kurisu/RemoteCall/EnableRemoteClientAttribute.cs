using System;
using Kurisu.Proxy.Attributes;

namespace Kurisu.RemoteCall;

/// <summary>
/// 启用远程调用
/// </summary>
[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
public sealed class EnableRemoteClientAttribute : AopAttribute
{
    public EnableRemoteClientAttribute()
    {
    }

    /// <summary>
    /// client name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// base url
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Type FallbackType { get; set; }
}


