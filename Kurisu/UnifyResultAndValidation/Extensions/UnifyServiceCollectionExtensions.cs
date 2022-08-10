using Kurisu.UnifyResultAndValidation.Filters;
using Kurisu.UnifyResultAndValidation;
using Kurisu.UnifyResultAndValidation.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class UnifyServiceCollectionExtensions
    {
        /// <summary>
        /// 统一格式处理，使用默认返回类<see cref="DefaultApiResult{T}"/>包装结果
        /// </summary>                        
        /// <param name="services">服务容器</param>
        /// <returns></returns>
        public static IServiceCollection AddKurisuUnifyResult(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IApiResult), typeof(DefaultApiResult<object>));

            services.AddMvcFilter<ValidateAndPackResultFilter>();
            // services.AddMvcFilter<ExceptionPackFilter>();

            return services;
        }
    }
}