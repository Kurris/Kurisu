using Kurisu.ConfigurableOptions.Attributes;
using Microsoft.Extensions.Options;

namespace Kurisu.Authentication;

[Configuration]
public class JwtSetting : IPostConfigureOptions<JwtSetting>
{
    public string TokenSecretKey { get; set; } = "12345";
    public string Issuer { get; set; }

    /// <summary>
    ///
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