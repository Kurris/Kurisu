using System.ComponentModel.DataAnnotations.Schema;
using Kurisu.DataAccessor.Entity;

namespace WebApiDemo.Entities
{
    [Table("goods")]
    public class Good : BaseEntity<int>
    {
        public string GoodsName { get; set; }
        public string GoodsDesc { get; set; }
    }
}
