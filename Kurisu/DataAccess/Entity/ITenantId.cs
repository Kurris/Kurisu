namespace Kurisu.DataAccess.Entity;

/// <summary>
/// 租户Id
/// </summary>
public interface ITenantId<T>
{
    /// <summary>
    /// 租户id
    /// </summary>
    public T TenantId { get; set; }
}