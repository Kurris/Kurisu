using System.Threading.Tasks;
using Kurisu.Gateway.Dto.Input;
using Kurisu.Gateway.Dto.Output;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Gateway.Services.Interfaces
{
    public interface IGlobalConfigurationService : ISingletonDependency
    {
        /// <summary>
        /// 保存全局设置
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        Task SaveGlobalConfiguration(SaveGlobalConfigurationInput input);

        /// <summary>
        /// 获取全局设置
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        Task<GetGlobalConfigurationOutput> GetGlobalConfiguration();
    }
}
