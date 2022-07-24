using System.Collections.Generic;
using System.Linq;
using Kurisu.Utils.Extensions;

namespace Kurisu.Gateway.Dto.Output
{
    public class GetAllRoutesOutput
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public string DownstreamPathTemplate { get; set; }
        public string UpstreamPathTemplate { get; set; }
        public string UpstreamHttpMethod { get; set; }

        public List<string> UpstreamHttpMethodList => UpstreamHttpMethod.Split(",").ToList();

        public string DownstreamScheme { get; set; }

        public string UpstreamHost { get; set; }
        public int Priority { get; set; } = 1;
        public int Timeout { get; set; }

        public string DownstreamHttpVersion { get; set; }

        public List<GetHostPortOutput> HostPort { get; set; }
    }
}
