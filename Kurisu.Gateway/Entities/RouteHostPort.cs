using System.ComponentModel.DataAnnotations.Schema;
using Kurisu.DataAccessor.Entity;

namespace Kurisu.Gateway.Entities
{
    [Table("RouteHostPorts")]
    public class RouteHostPort : BaseEntity<int>
    {
        public int RouteId { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
    }
}
