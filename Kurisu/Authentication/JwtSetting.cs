namespace Kurisu.Authentication
{
    public class JwtSetting
    {
        public string TokenName { get; set; }
        public string TokenSecretKey { get; set; }
        public string Issuer { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// second
        /// </summary>
        public int Expiration { get; set; }
    }
}