using System.Reflection;

namespace Kurisu.DataAccessor.Resolvers.Abstractions
{
    /// <summary>
    /// 模型配置来源处理器
    /// </summary>
    public interface IModelConfigurationSourceResolver
    {
        /// <summary>
        /// 获取配置类所在的程序集
        /// </summary>
        /// <returns></returns>
        Assembly GetSourceAssembly();
    }
}