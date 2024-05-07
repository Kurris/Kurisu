using System.ComponentModel.DataAnnotations;
using Kurisu.AspNetCore.ConfigurableOptions.Attributes;

namespace Kurisu.AspNetCore.Authentication.Options;

/// <summary>
/// jwt配置
/// </summary>
[Configuration]
public class JwtOptions
{
    /// <summary>
    /// 密钥
    /// </summary>
    [Required(ErrorMessage = "{0}密钥不可为空")]
    [MinLength(15, ErrorMessage = "{0}密钥不能小于15位")]
    public string SecretKey { get; set; }

    /// <summary>
    /// 颁发者
    /// </summary>
    public string Issuer { get; set; }

    /// <summary>
    /// 受众
    /// </summary>
    public string Audience { get; set; }

    /// <summary>
    /// second
    /// </summary>
    public int Expiration { get; set; } = 7200;
}