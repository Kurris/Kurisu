using System.Collections.Generic;

namespace Kurisu.Authentication
{
    public class SwaggerOAuthSetting
    {
        public bool Enable { get; set; }
        public string Authority { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public Dictionary<string, string> Scopes { get; set; }
    }
}