namespace Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;

/// <summary>
/// 租户id
/// </summary>
public interface ITenantId
{
    /// <summary>
    /// 租户唯一值
    /// </summary>
    public string TenantId { get; set; }
}