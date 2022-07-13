using Kurisu.UnifyResultAndValidation.Filters;
using Kurisu.UnifyResultAndValidation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class UnifyServiceCollectionExtensions
    {
        /// <summary>
        /// 统一格式处理，使用默认返回类<see cref="ApiResult{T}"/>包装结果
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