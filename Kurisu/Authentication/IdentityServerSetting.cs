using Kurisu.ConfigurableOptions.Attributes;

namespace Kurisu.Authentication
{
    public class IdentityServerSetting
    {
        public bool RequireHttpsMetadata { get; set; }
        public string Authority { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public PatSetting Pat { get; set; }
    }
}