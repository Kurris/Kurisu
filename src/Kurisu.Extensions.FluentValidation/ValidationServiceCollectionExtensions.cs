using Kurisu.Extensions.FluentValidation.Aop;
using Kurisu.Extensions.FluentValidation.Internal;
using System.Reflection;
using AspectCore.Configuration;
using AspectCore.DynamicProxy;
using AspectCore.Extensions.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Extensions.FluentValidation;

/// <summary>
/// 为包含可验证模型参数的方法启用自动验证.
/// </summary>
public static class ValidationServiceCollectionExtensions
{
    /// <summary>
    /// 启用模型参数验证, 默认在首个无效参数处停止; 重复注册保留已有策略.
    /// </summary>
    /// <param name="services">服务集合.</param>
    /// <returns>用于继续注册服务的服务集合.</returns>
    public static IServiceCollection AddParameterValidation(this IServiceCollection services)
        => Configure(services, null);

    /// <summary>
    /// 选择是否收集全部模型参数的错误; 不合并配置错误和取消异常.
    /// </summary>
    /// <param name="services">服务集合.</param>
    /// <param name="collectAllErrors">是否收集所有模型参数的字段错误.</param>
    /// <returns>用于继续注册服务的服务集合.</returns>
    public static IServiceCollection AddParameterValidation(this IServiceCollection services, bool collectAllErrors)
        => Configure(services, collectAllErrors);

    private static IServiceCollection Configure(IServiceCollection services, bool? collectAllErrors)
    {
        ArgumentNullException.ThrowIfNull(services);
        // 在代理筛选阶段匹配参数类型, 普通方法不因验证功能而被代理.
        services.ConfigureDynamicProxy(config =>
        {
            var factory = config.Interceptors.OfType<ValidationInterceptorFactory>().SingleOrDefault();
            if (factory is null)
            {
                factory = new ValidationInterceptorFactory();
                config.Interceptors.Add(factory);
            }

            // 重复的默认注册保留已配置的策略, 显式配置以最后一次为准.
            if (collectAllErrors.HasValue) factory.CollectAllErrors = collectAllErrors.Value;
        });
        return services;
    }

    private static bool RequiresValidation(MethodInfo method)
        // 验证器自身不进入参数验证切面, 避免校验时再次验证同一个模型.
        => !typeof(IValidator).IsAssignableFrom(method.DeclaringType)
           && method.GetParameters().Any(ValidationParameter.RequiresValidation);

    private sealed class ValidationInterceptorFactory() : InterceptorFactory(RequiresValidation)
    {
        public bool CollectAllErrors { get; set; }

        public override IInterceptor CreateInstance(IServiceProvider serviceProvider)
            => new ParameterValidationInterceptor(CollectAllErrors);
    }
}