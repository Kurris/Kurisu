// ReSharper disable ClassNeverInstantiated.Global
namespace Kurisu.Authentication;

/// <summary>
/// 个人访问token配置
/// </summary>
public class PatSetting
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// 客户端id
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// 客户端密钥
    /// </summary>
    public string ClientSecret { get; set; }
}