using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor
{
    /// <summary>
    /// 数据访问builder
    /// </summary>
    internal class KurisuDataAccessorBuilder : IKurisuDataAccessorBuilder
    {
        /// <summary>
        /// 服务容器
        /// </summary>
        public IServiceCollection Services { get; set; }

        /// <summary>
        /// 是否为读写分离
        /// </summary>
        public bool IsReadWriteSplit { get; set; }
    }
}