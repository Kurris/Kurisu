using Kurisu.DataAccessor.Entity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Manon.Gateway.ApplicationCore.Entities
{
    [Table("GlobalConfigurations")]
    public class GlobalConfiguration : BaseEntity<int>
    {
        public string DownstreamHttpVersion { get; set; }
    }
}
