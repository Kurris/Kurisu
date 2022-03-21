using System;
using Kurisu.DependencyInjection.Attributes;

namespace Kurisu.ConfigurableOptions.Attributes
{
    /// <summary>
    /// AppSetting.json 映射
    /// </summary>
    [SkipScan]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ConfigurationAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="section">节点路径</param>
        public ConfigurationAttribute(string section)
        {
            Section = section;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ConfigurationAttribute() : this(string.Empty)
        {
        }

        /// <summary>
        /// 节点路径
        /// </summary>
        public string Section { get; }
    }
}