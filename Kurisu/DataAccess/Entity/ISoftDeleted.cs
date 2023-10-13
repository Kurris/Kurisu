namespace Kurisu.DataAccess.Entity;

/// <summary>
/// 是否软删除
/// </summary>
public interface ISoftDeleted
{
    /// <summary>
    /// 是否软删除
    /// </summary>
    /// <remarks>
    /// 如果主动设置为true,那么在调用删除时,将视为物理删除
    /// </remarks>
    bool IsDeleted { get; set; }
}