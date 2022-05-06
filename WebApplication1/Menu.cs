using System.ComponentModel.DataAnnotations.Schema;
using Kurisu.DataAccessor.Entity;

namespace WebApplication1
{
    [Table("menu")]
    public class Menu : BaseEntity<int>
    {
        public string Code { get; set; }
        public string PCode { get; set; }
        public string DisplayName { get; set; }
        public string Route { get; set; }
        public string Icon { get; set; }
        public bool Visible { get; set; }
    }
}