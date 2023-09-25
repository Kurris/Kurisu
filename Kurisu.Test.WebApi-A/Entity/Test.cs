using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kurisu.DataAccess;
using Kurisu.DataAccess.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kurisu.Test.WebApi_A.Entity;

[EnableSplitTable]
[Table("test")]
public class Test : BaseEntity, IEntityTypeConfiguration<Test>
{
    [Column(Order = 0)]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public override long Id { get; set; }
    // public string Address { get; set; }

    public string Name { get; set; }
    // public List<int> Data { get; set; }

    public void Configure(EntityTypeBuilder<Test> builder)
    {
        // builder.Property(x => x.Data).HasJsonConversion();
    }
}