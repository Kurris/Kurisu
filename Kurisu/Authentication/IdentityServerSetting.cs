using System.ComponentModel.DataAnnotations;
using Kurisu.ConfigurableOptions.Attributes;

namespace Kurisu.Authentication;

/// <summary>
/// identity server 配置
/// </summary>
[Configuration]
public class IdentityServerSetting
{
    /// <summary>
    /// 是否需要https
    /// </summary>
    public bool RequireHttpsMetadata { get; set; }

    /// <summary>
    /// 授权地址
    /// </summary>
    [Required(ErrorMessage = "{0}授权地址不能为空")]
    public string Authority { get; set; }

    /// <summary>
    /// 签发人
    /// </summary>
    public string Issuer { get; set; }

    /// <summary>
    /// 受众
    /// </summary>
    public string Audience { get; set; }

    /// <summary>
    /// 个人访问token
    /// </summary>
    public PatSetting Pat { get; set; }
}