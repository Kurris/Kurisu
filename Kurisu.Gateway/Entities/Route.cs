using Kurisu.DataAccessor.Entity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kurisu.Gateway.Entities
{
    [Table("Routes")]
    public class Route : BaseEntity<int>
    {
        public int ProjectId { get; set; }
        public bool Enable { get; set; }

        public string DownstreamPathTemplate { get; set; }
        public string UpstreamPathTemplate { get; set; }
        public string UpstreamHttpMethod { get; set; }

        public string DownstreamScheme { get; set; }

        public int Priority { get; set; } = 1;
        public int Timeout { get; set; }

        public string DownstreamHttpVersion { get; set; }
    }
}
