namespace Kurisu.DataAccessor.Functions.Default.Abstractions
{
    /// <summary>
    /// 是否开启软删除
    /// </summary>
    public interface IDbContextSoftDeleted
    {
        bool IsEnableSoftDeleted { get; set; }
    }
}