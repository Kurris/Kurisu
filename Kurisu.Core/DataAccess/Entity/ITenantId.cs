namespace Kurisu.Core.DataAccess.Entity;

/// <summary>
/// 租户id
/// </summary>
public interface ITenantId
{
    public string TenantId { get; set; }
}