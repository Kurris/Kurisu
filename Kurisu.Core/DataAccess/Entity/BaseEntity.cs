namespace Kurisu.Core.DataAccess.Entity;

/// <summary>
/// 基础实体
/// </summary>
/// <typeparam name="TId"></typeparam>
/// <typeparam name="TUser"></typeparam>
public abstract class BaseEntity<TId, TUser>
{
    public abstract TId Id { get; set; }

    public abstract TUser CreatedBy { get; set; }

    public abstract DateTime CreateTime { get; set; }

    public abstract TUser ModifiedBy { get; set; }

    public abstract DateTime ModifiedTime { get; set; }
}