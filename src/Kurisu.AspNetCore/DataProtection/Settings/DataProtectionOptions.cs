namespace Kurisu.AspNetCore.DataProtection.Settings;

/// <summary>
/// 数据保存配置
/// </summary>
public class DataProtectionOptions
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// 应用名称
    /// </summary>
    public string AppName { get; set; }

    /// <summary>
    /// 持续化提供器
    /// </summary>
    public DataProtectionProviderType Provider { get; set; }
}