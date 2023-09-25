using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccess.Entity;

/**
 * 生成列排序: Id Column(Order = 0) 主键--子类属性--Base属性
 *
 *
 */
/// <summary>
/// 基础实体
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class BaseEntity<TKey> : IBaseEntity
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key, Comment("主键"), Column(Order = 0)]
    public virtual TKey Id { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    [Comment("创建人")]
    [Required(ErrorMessage = "创建人不能为空")]
    [Range(0, int.MaxValue)]
    public int CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Comment("创建时间")]
    [Column(TypeName = "datetime(3)")]
    [Required(ErrorMessage = "创建时间不能为空")]
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    [Comment("修改人")]
    public int? ModifiedBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [Comment("修改时间")]
    [Column(TypeName = "datetime(3)")]
    public DateTime? ModifiedTime { get; set; }
}

/// <summary>
/// 基础实体
/// </summary>
public abstract class BaseEntity : BaseEntity<long>, ISoftDeleted
{
    [Required]
    [Column(TypeName = "bit(1)"), Comment("是否逻辑删除")]
    public bool IsDeleted { get; set; }
}

public static class BaseEntityConstants
{
    public const string Id = nameof(BaseEntity.Id);

    public const string CreatedBy = nameof(BaseEntity.CreatedBy);

    public const string CreateTime = nameof(BaseEntity.CreateTime);

    public const string ModifiedBy = nameof(BaseEntity.ModifiedBy);

    public const string ModifiedTime = nameof(BaseEntity.ModifiedTime);

    public const string IsDeleted = nameof(BaseEntity.IsDeleted);
}