using Kurisu.Utils.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Kurisu.Gateway.Dto.Output
{
    public class GetRoutesOutput
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }
        public string DownstreamPathTemplate { get; set; }
        public string UpstreamPathTemplate { get; set; }
        public string UpstreamHttpMethod { get; set; }

        public List<string> UpstreamHttpMethodList => UpstreamHttpMethod.ToObject<List<string>>();

        public bool RouteIsCaseSensitive { get; set; }

        public string DownstreamScheme { get; set; }

        public string UpstreamHost { get; set; }
    
        public int Priority { get; set; } = 1;
        public int Timeout { get; set; }
   

        public bool Enable { get; set; }

        public string DownstreamHttpVersion { get; set; }

        public List<GetHostPortOutput> HostPort { get; set; }
    }
}
