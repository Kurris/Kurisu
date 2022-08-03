namespace Kurisu.DataAccessor.Abstractions
{
    /// <summary>
    /// dbContext接口
    /// </summary>
    public interface IAppDbContext
    {
        /// <summary>
        /// 是否自动提交
        /// </summary>
        public bool IsAutomaticSaveChanges { get; set; }
    }
}