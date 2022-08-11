using System.Reflection;
using Kurisu.DataAccessor.Resolvers.Abstractions;

namespace Kurisu.DataAccessor.Resolvers
{
    /// <summary>
    /// 默认模型配置所在程序集来源处理器(no-op)
    /// </summary>
    public class DefaultModelConfigurationSourceResolver : IModelConfigurationSourceResolver
    {
        /// <summary>
        /// 获取来源程序集
        /// </summary>
        /// <returns></returns>
        public virtual Assembly GetSourceAssembly()
        {
            return null;
        }
    }
}