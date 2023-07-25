using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kurisu.DataAccessor.Entity;

/// <summary>
/// 基础实体
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TUserId">用户id类型</typeparam>
public abstract class BaseEntity<TKey, TUserId>
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    public TKey Id { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    [Required(ErrorMessage = "创建人不能为空")]
    [Range(0, int.MaxValue)]
    public TUserId CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column(TypeName = "datetime(3)")]
    [Required(ErrorMessage = "创建时间不能为空")]
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public TUserId ModifiedBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [Column(TypeName = "datetime(3)")]
    public DateTime? ModifiedTime { get; set; }
}


public static class BaseEntityConstants
{
    public const string Id = nameof(BaseEntity<object, object>.Id);

    public const string CreatedBy = nameof(BaseEntity<object, object>.CreatedBy);

    public const string CreateTime = nameof(BaseEntity<object, object>.CreateTime);

    public const string ModifiedBy = nameof(BaseEntity<object, object>.ModifiedBy);

    public const string ModifiedTime = nameof(BaseEntity<object, object>.ModifiedTime);
}