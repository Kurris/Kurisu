using Kurisu.ConfigurableOptions.Attributes;

namespace Kurisu.Authorization
{
    [Configuration]
    public class JwtAppSetting
    {
        public string TokenName { get; set; }
        public string TokenSecretKey { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        
        public int Expiration { get; set; }
    }
}