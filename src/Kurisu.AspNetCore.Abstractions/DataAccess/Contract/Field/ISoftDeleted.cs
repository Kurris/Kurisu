namespace Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;

/// <summary>
/// 软删除
/// </summary>
public interface ISoftDeleted
{
    /// <summary>
    /// 删除标识
    /// </summary>
    bool IsDeleted { get; set; }
}