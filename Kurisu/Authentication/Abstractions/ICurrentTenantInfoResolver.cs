namespace Kurisu.Authentication.Abstractions;

/// <summary>
/// 租户信息获取处理器
/// </summary>
public interface ICurrentTenantInfoResolver
{
    /// <summary>
    /// Claims:tenant; header:X-Requested-TenantId
    /// <remarks>
    /// 默认:tenant
    /// </remarks>
    /// </summary>
    string TenantKey { get; }

    /// <summary>
    /// 获取租户id
    /// </summary>
    /// <returns></returns>
    int GetTenantId();
}