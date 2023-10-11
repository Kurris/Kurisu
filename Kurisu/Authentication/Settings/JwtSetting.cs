using System.ComponentModel.DataAnnotations;
using Kurisu.ConfigurableOptions.Attributes;
using Microsoft.Extensions.Options;

namespace Kurisu.Authentication.Settings;

[Configuration]
public class JwtSetting : IPostConfigureOptions<JwtSetting>
{
    /// <summary>
    /// ��Կ
    /// </summary>
    [Required(ErrorMessage = "{0}��Կ����Ϊ��")]
    [MinLength(15, ErrorMessage = "{0}��Կ����С��15λ")]
    public string SecretKey { get; set; }

    /// <summary>
    /// �䷢��
    /// </summary>
    public string Issuer { get; set; }

    /// <summary>
    /// ����
    /// </summary>
    public string Audience { get; set; }

    /// <summary>
    /// second
    /// </summary>
    public int Expiration { get; set; } = 7200;


    public void PostConfigure(string name, JwtSetting options)
    {
    }
}