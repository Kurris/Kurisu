using Kurisu.UnifyResultAndValidation.Filters;
using Kurisu.UnifyResultAndValidation;
using Kurisu.UnifyResultAndValidation.Abstractions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 返回值包装扩展
/// </summary>
[SkipScan]
public static class UnifyServiceCollectionExtensions
{
    /// <summary>
    /// 统一格式处理，使用默认返回类<see cref="DefaultApiResult{T}"/>包装结果
    /// </summary>
    /// <param name="services">服务容器</param>
    /// <param name="wrapException">是否控制器方法中包装错误异常</param>
    /// <returns></returns>
    public static IServiceCollection AddKurisuUnifyResult(this IServiceCollection services, bool wrapException = true)
    {
        services.AddSingleton(typeof(IApiResult), typeof(DefaultApiResult<object>));
        services.AddMvcFilter<ValidateAndPackResultFilter>();

        if (wrapException)
            services.AddMvcFilter<ExceptionPackFilter>();

        return services;
    }
}