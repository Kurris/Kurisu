using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccess.Internal;

[Table("__AutoMigrationsHistory")]
public class AutoMigrationsHistory
{
    [Key]
    public int Id { get; set; }

    [Comment("代码快照")]
    [Required]
    public string SnapshotDefine { get; set; }


    [Column(TypeName = "datetime(3)"), Comment("迁移时间")]
    public DateTime MigrationTime { get; set; }
}