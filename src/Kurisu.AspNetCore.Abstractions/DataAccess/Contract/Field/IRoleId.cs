namespace Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;

/// <summary>
/// 角色id
/// </summary>
public interface IRoleId
{
    /// <summary>
    /// 角色唯一值
    /// </summary>
    public string RoleId { get; set; }
}