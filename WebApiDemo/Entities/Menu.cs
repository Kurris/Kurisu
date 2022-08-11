using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Kurisu.DataAccessor.Entity;
using Kurisu.DataAccessor.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WebApiDemo.Entities
{
    [Table("menus")]
    public class Menu : BaseEntity<int>, ISoftDeleted
    {
        public string Code { get; set; }
        public string PCode { get; set; }
        public string DisplayName { get; set; }
        public string Route { get; set; }
        public string Icon { get; set; }
        public bool Visible { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class BlogEntityTypeConfiguration : IEntityTypeConfiguration<Menu>
    {
        public void Configure(EntityTypeBuilder<Menu> builder)
        {
        }
    }


    public class HereDefaultModelConfigurationSourceResolver : DefaultModelConfigurationSourceResolver
    {
        public override Assembly GetSourceAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }
    }
}