using System.ComponentModel.DataAnnotations.Schema;
using Kurisu.DataAccessor.Entity;

namespace Kurisu.Gateway.Entities
{
    [Table("Projects")]
    public class Project : BaseEntity<int>
    {
        public string Name { get; set; }

        public int OrderNo { get; set; }

        public bool Enable { get; set; }
    }
}
