using Kurisu.Core.DataAccess.Entity;
using Kurisu.SqlSugar.Attributes;
using SqlSugar;

namespace Kurisu.SqlSugar;

public class SugarBaseEntity : BaseEntity<long, int>
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public override long Id { get; set; }

    [InsertUserGeneration]
    public override int CreatedBy { get; set; }

    [InsertDateTimeGeneration]
    public override DateTime CreateTime { get; set; }


    [UpdateUserGeneration]
    [InsertUserGeneration]
    public override int ModifiedBy { get; set; }

    [UpdateDateTimeGeneration]
    [InsertDateTimeGeneration]
    public override DateTime ModifiedTime { get; set; }
}
