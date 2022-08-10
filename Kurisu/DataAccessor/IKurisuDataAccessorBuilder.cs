using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor
{
    /// <summary>
    /// 数据访问builder
    /// </summary>
    public interface IKurisuDataAccessorBuilder
    {
        /// <summary>
        /// 服务容器
        /// </summary>
        public IServiceCollection Services { get; set; }
    }
}