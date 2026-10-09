using Kurisu.Extensions.FluentValidation.Abstractions;
using Kurisu.Extensions.FluentValidation.Internal;
using Kurisu.Extensions.FluentValidation.Result;
using FluentValidation;
using FluentValidation.Results;

namespace Kurisu.Extensions.FluentValidation;

/// <summary>
/// 在模型中定义验证规则, 并组合实现验证接口的嵌套模型.
/// </summary>
public static class ModelValidation
{
    /// <summary>
    /// 显式验证模型, 失败时抛出与 AOP 相同的 ParameterValidationException.
    /// 可用于消息消费、后台任务等没有代理入口的调用.
    /// </summary>
    /// <param name="model">实现验证接口的模型.</param>
    /// <param name="serviceProvider">当前调用作用域的服务提供者.</param>
    /// <param name="cancellationToken">当前调用的取消令牌.</param>
    public static async Task ValidateAsync<T>(T model, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default) where T : IAopValidatableObject
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        var errors = await ParameterValidation.ValidateAsync(serviceProvider, nameof(model), model, cancellationToken);
        if (errors.Length > 0) throw new ParameterValidationException(errors);
    }

    /// <summary>
    /// 创建并配置当前模型的内联验证器.
    /// </summary>
    /// <typeparam name="T">需要验证的模型类型.</typeparam>
    /// <param name="configure">模型的 FluentValidation 规则配置.</param>
    public static InlineValidator<T> Create<T>(Action<InlineValidator<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var validator = new InlineValidator<T>();
        configure(validator);
        return validator;
    }

    /// <summary>
    /// 创建嵌套模型验证器, 使用原生子验证上下文保留路径、根数据和错误信息.
    /// </summary>
    /// <typeparam name="T">定义自身验证规则的模型类型.</typeparam>
    /// <param name="serviceProvider">当前调用作用域的服务提供者.</param>
    public static IValidator<T> For<T>(IServiceProvider serviceProvider) where T : IAopValidatableObject
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        return new DelegatingModelValidator<T>(serviceProvider);
    }

    internal static IValidator ResolveValidator(IAopValidatableObject model, IServiceProvider serviceProvider,
        string modelName)
    {
        var validator = model.CreateValidator(serviceProvider)
                        ?? throw new InvalidOperationException($"{modelName} 未提供有效的验证器.");
        var modelType = model.GetType();
        return validator.CanValidateInstancesOfType(modelType)
            ? validator
            : throw new InvalidOperationException($"{modelName} 的验证器不支持模型类型 {modelType.Name}.");
    }

    // 在实际验证时解析模型的验证器, 不缓存模型或当前作用域中的依赖.
    private sealed class DelegatingModelValidator<T>(IServiceProvider serviceProvider) : AbstractValidator<T>
        where T : IAopValidatableObject
    {
        public override ValidationResult Validate(ValidationContext<T> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return GetValidator(context.InstanceToValidate).Validate(context);
        }

        public override async Task<ValidationResult> ValidateAsync(ValidationContext<T> context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();
            var result = await GetValidator(context.InstanceToValidate).ValidateAsync(context, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        private IValidator GetValidator(T model)
        {
            ArgumentNullException.ThrowIfNull(model);
            return ResolveValidator(model, serviceProvider, typeof(T).Name);
        }
    }
}