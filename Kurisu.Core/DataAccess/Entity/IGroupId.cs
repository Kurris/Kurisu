namespace Kurisu.Core.DataAccess.Entity;

/// <summary>
/// 分组列
/// </summary>
public interface IGroupId
{
    /// <summary>
    /// 分组唯一值
    /// </summary>
    public string GroupId { get; set; }
}