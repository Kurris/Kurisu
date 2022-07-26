using System.Collections.Generic;

namespace Kurisu.Gateway.Dto.Input
{
    public class SaveRouteInput
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string DownstreamPathTemplate { get; set; }
        public string UpstreamPathTemplate { get; set; }

        public List<string> UpstreamHttpMethodList { get; set; } = new()
        {
            "GET", "POST", "PUT", "DELETE", "OPTIONS"
        };

        public string DownstreamScheme { get; set; }

        public int Priority { get; set; } = 1;
        public int Timeout { get; set; }

        public string DownstreamHttpVersion { get; set; }

        public List<SaveHostPortInput> HostPort { get; set; }

        public bool Enable { get; set; }
    }


    public class SaveHostPortInput
    {
        public string Host { get; set; }

        public int Port { get; set; }
    }
}