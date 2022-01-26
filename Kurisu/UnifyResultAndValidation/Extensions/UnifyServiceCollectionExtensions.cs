using Kurisu.MVC.Extensions;
using Kurisu.UnifyResultAndValidation.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.UnifyResultAndValidation.Extensions
{
    public static class UnifyServiceCollectionExtensions
    {
        /// <summary>
        /// 统一格式处理
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddUnify(this IServiceCollection services)
        {
            services.AddMvcFilter<ValidateAndPackResultFilter>();
            return services;
        }
    }
}