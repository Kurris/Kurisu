namespace Kurisu.DataAccessor.Entity
{
    /// <summary>
    /// 租户
    /// </summary>
    /// <typeparam name="TKey">TenantId类型</typeparam>
    public interface ITenantId<TKey>
    {
        /// <summary>
        /// 租户id
        /// </summary>
        public TKey TenantId { get; set; }
    }
}