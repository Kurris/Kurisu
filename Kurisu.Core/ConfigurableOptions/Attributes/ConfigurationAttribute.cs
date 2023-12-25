using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Core.ConfigurableOptions.Attributes;

/// <summary>
/// 配置文件映射
/// </summary>
[SkipScan]
[AttributeUsage(AttributeTargets.Class)]
public sealed class ConfigurationAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigurationAttribute() : this(string.Empty)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="path">节点路径</param>
    public ConfigurationAttribute(string path)
    {
        Path = path;
    }

    /// <summary>
    /// 节点路径
    /// </summary>
    public string Path { get; }
}