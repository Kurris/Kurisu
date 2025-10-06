// ReSharper disable ClassNeverInstantiated.Global

using Kurisu.AspNetCore.Abstractions.ConfigurableOptions;

namespace Kurisu.AspNetCore.Authentication.Options;

/// <summary>
/// 个人访问token配置
/// </summary>
[Configuration("IdentityServerOptions:Pat")]
public class PatOptions
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// 验证scheme
    /// </summary>
    public string Scheme { get; set; }

    /// <summary>
    /// 客户端id
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// 客户端密钥
    /// </summary>
    public string ClientSecret { get; set; }
}