using Kurisu.Core.ConfigurableOptions.Attributes;

namespace Kurisu.AspNetCore.DataProtection.Settings;

/// <summary>
/// 数据保存配置
/// </summary>
[Configuration]
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
    /// 缓存key
    /// </summary>
    public string Key { get; set; } = "DataProtection-Keys";
}
