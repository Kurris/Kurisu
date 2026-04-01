namespace Kurisu.AspNetCore.Abstractions.DataAccess.Contract;

/// <summary>
/// 基础实体
/// </summary>
/// <typeparam name="TId"></typeparam>
/// <typeparam name="TUser"></typeparam>
public abstract class BaseEntity : IEntity
{
    /// <summary>
    /// 主键
    /// </summary>
    public abstract long Id { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public abstract int CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public abstract DateTime CreateTime { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public abstract int ModifiedBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public abstract DateTime ModifiedTime { get; set; }
}