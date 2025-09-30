using Kurisu.AspNetCore.UnifyResultAndValidation.Abstractions;
using Kurisu.AspNetCore.UnifyResultAndValidation.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.AspNetCore.UnifyResultAndValidation.Extensions;

/// <summary>
/// 返回值包装扩展
/// </summary>
[SkipScan]
public static class UnifyServiceCollectionExtensions
{
    /// <summary>
    /// 统一格式处理，使用默认返回类<see cref="DefaultApiResult"/>包装结果
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <param name="wrapException">是否控制器方法中包装错误异常</param>
    /// <returns></returns>
    public static IServiceCollection AddUnifyResult(this IServiceCollection services, bool wrapException = true)
    {
        services.AddScoped<ApiRequestSettingService>();

        services.TryAddSingleton(typeof(IApiResult), typeof(ApiResult<object>));
        services.AddMvcFilter<ValidateAndPackResultFilter>();

        if (wrapException)
            services.AddMvcFilter<ExceptionPackFilter>();

        return services;
    }
}