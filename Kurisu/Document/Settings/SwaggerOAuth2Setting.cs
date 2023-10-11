using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Kurisu.ConfigurableOptions.Attributes;

namespace Kurisu.Document.Settings;

/// <summary>
/// swagger oauth2.0 配置
/// </summary>
[Configuration]
public class SwaggerOAuth2Setting : IValidatableObject
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// 授权地址
    /// </summary>
    public string Authority { get; set; }

    /// <summary>
    /// 客户端id
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// 客户端密钥
    /// </summary>
    public string ClientSecret { get; set; }

    /// <summary>
    /// 授权作用域
    /// </summary>
    public Dictionary<string, string> Scopes { get; set; }

    /// <summary>
    /// 验证
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enable)
            yield break;

        if (string.IsNullOrEmpty(Authority))
        {
            yield return new ValidationResult("授权地址不能为空", new[] { nameof(Authority) });
        }

        if (string.IsNullOrEmpty(ClientId))
        {
            yield return new ValidationResult("客户端不能为空", new[] { nameof(ClientId) });
        }

        if (string.IsNullOrEmpty(ClientSecret))
        {
            yield return new ValidationResult("客户端密钥不能为空", new[] { nameof(ClientSecret) });
        }

        if (Scopes?.Any() != true || !Scopes.ContainsKey("openid"))
        {
            yield return new ValidationResult("授权作用域必须包括openid", new[] { nameof(Scopes) });
        }
    }
}