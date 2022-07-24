namespace Kurisu.Gateway.Dto.Input
{
    public class SaveGlobalConfigurationInput
    {
        public int Id { get; set; }

        public string BaseUrl { get; set; }

        public string DownstreamScheme { get; set; }

        public string DownstreamHttpVersion { get; set; }
    }
}
