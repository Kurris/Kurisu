namespace Kurisu.AspNetCore.DataProtection;

/// <summary>
/// 数据保护持久化类型
/// </summary>
public enum DataProtectionProviderType
{
    /// <summary>
    /// 数据库
    /// </summary>
    Db = 0,

    /// <summary>
    /// Redis
    /// </summary>
    Redis = 1
}