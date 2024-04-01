namespace Kurisu.Core.DataAccess.Entity;

/// <summary>
/// 基础实体
/// </summary>
/// <typeparam name="TId"></typeparam>
/// <typeparam name="TUser"></typeparam>
public abstract class BaseEntity<TId, TUser>
{
    /// <summary>
    /// 主键
    /// </summary>
    public abstract TId Id { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public abstract TUser CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public abstract DateTime CreateTime { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public abstract TUser ModifiedBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public abstract DateTime ModifiedTime { get; set; }
}