using System;

namespace Kurisu.ConfigurableOptions.Attributes
{
    /// <summary>
    /// appsetting.json 映射
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class ConfigurationAttribute : Attribute
    {
        /// <summary>
        /// 构造函数，使用sectionPath在appsetting.json进行搜索
        /// </summary>
        /// <param name="sectionPath">节点路径</param>
        public ConfigurationAttribute(string sectionPath)
        {
            SectionPath = sectionPath;
        }

        /// <summary>
        /// 构造函数,使用类型名为Section的Key在appsetting.json进行搜索
        /// </summary>
        public ConfigurationAttribute() : this(string.Empty)
        {
        }

        /// <summary>
        /// 节点路径
        /// </summary>
        public string SectionPath { get; }
    }
}