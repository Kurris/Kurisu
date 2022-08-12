namespace Kurisu.Authentication.Abstractions
{
    /// <summary>
    /// 租户信息获取处理器
    /// </summary>
    public interface ICurrentTenantInfoResolver
    {
        /// <summary>
        /// 租户id header请求key
        /// </summary>
        string TenantKey { get; }

        /// <summary>
        /// 获取租户id
        /// </summary>
        /// <returns></returns>
        int GetTenantId();
    }
}