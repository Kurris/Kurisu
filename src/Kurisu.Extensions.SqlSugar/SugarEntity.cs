using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using Kurisu.Extensions.SqlSugar.Attributes;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar;

/// <summary>
/// SqlSugar基础实体类
/// </summary>
public class SugarEntity : BaseEntity
{
    /// <summary>
    /// 主键id
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public override long Id { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    [InsertUserIdGeneration]
    public override int CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [InsertDateTimeGeneration]
    public override DateTime CreateTime { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    [UpdateUserIdGeneration]
    [InsertUserIdGeneration]
    public override int ModifiedBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [UpdateDateTimeGeneration]
    [InsertDateTimeGeneration]
    public override DateTime ModifiedTime { get; set; }
}

/// <summary>
/// 带租户Id的SqlSugar基础实体类 
/// </summary>
public class SugarEntityWithTenant : SugarEntity, ITenantId
{
    public string TenantId { get; set; }
}

public interface IIndexConfigurator
{
    List<IndexModel> GetIndexConfigs();
}

public class IndexModel
{
    public string IndexName { get; set; }

    public string[] ColumnNames { get; set; }

    public bool IsUnique { get; set; }
}