namespace Kurisu.DataAccessor.Entity
{
    /// <summary>
    /// 是否软删除
    /// </summary>
    public interface ISoftDelete
    {
        public bool IsSoftDeleted { get; set; }
    }
}