namespace Kurisu.DataAccessor.Functions.Default.Abstractions;

/// <summary>
/// DbContext软删除控制
/// </summary>
public interface IDbContextSoftDeleted
{
    /// <summary>
    /// 是否开启软删除
    /// </summary>
    bool IsEnableSoftDeleted { get; set; }
}