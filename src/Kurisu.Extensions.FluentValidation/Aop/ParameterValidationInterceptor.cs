using Kurisu.Extensions.FluentValidation.Internal;
using Kurisu.Extensions.FluentValidation.Result;
using System.Collections.Concurrent;
using System.Reflection;
using AspectCore.DynamicProxy;

namespace Kurisu.Extensions.FluentValidation.Aop;

/// <summary>
/// 在默认事务前验证实现 IAopValidatableObject 的模型参数.
/// </summary>
public sealed class ParameterValidationInterceptor(bool collectAllErrors) : AbstractInterceptor
{
    // 仅缓存方法元数据, 不持有模型、验证器或调用作用域中的服务.
    private static readonly ConcurrentDictionary<(MethodInfo Service, MethodInfo Implementation), ValidationParameter[]> _plans = new();

    /// <summary>
    /// 默认在第一个无效参数处停止验证.
    /// </summary>
    public ParameterValidationInterceptor() : this(false)
    {
    }

    /// <summary>
    /// 保持验证先于默认 Order 为 0 的事务切面.
    /// </summary>
    public override int Order { get; set; } = -9;

    /// <summary>
    /// 每个输入参数只验证一次, 全部通过后再执行后续切面.
    /// </summary>
    public override Task Invoke(AspectContext context, AspectDelegate next)
    {
        var parameters = _plans.GetOrAdd((context.ServiceMethod, context.ImplementationMethod),
            static methods => CreatePlan(methods.Service, methods.Implementation));
        // 闭合泛型方法可能没有可验证参数, 直接进入后续方法而不创建异步状态机.
        if (parameters.Length == 0)
            return next(context);

        var cancellationToken = GetCancellationToken(context.Parameters);
        // 已取消的调用在进入异步验证流程前终止.
        cancellationToken.ThrowIfCancellationRequested();

        return InvokeValidatedAsync(context, next, parameters, collectAllErrors, cancellationToken);
    }

    private static ValidationParameter[] CreatePlan(MethodInfo serviceMethod, MethodInfo implementationMethod)
    {
        var serviceParameters = serviceMethod.GetParameters();
        var implementationParameters = implementationMethod.GetParameters();
        var parameters = new List<ValidationParameter>();
        for (var i = 0; i < serviceParameters.Length; i++)
        {
            if (ValidationParameter.Create(serviceParameters[i], implementationParameters[i]) is { } parameter)
                parameters.Add(parameter);
        }

        return [.. parameters];
    }

    private static CancellationToken GetCancellationToken(object[] arguments)
    {
        // 保持取第一个实际取消令牌的行为, 包括声明为 object 的参数.
        foreach (var argument in arguments)
            if (argument is CancellationToken token)
                return token;

        return CancellationToken.None;
    }

    private static async Task InvokeValidatedAsync(AspectContext context, AspectDelegate next,
        ValidationParameter[] parameters, bool collectAllErrors, CancellationToken cancellationToken)
    {
        List<ParameterValidationError>? collectedErrors = null;
        foreach (var parameter in parameters)
        {
            var errors = await ParameterValidation.ValidateAsync(context.ServiceProvider, parameter.Name,
                context.Parameters[parameter.Position], cancellationToken);
            if (errors.Length == 0) continue;
            if (!collectAllErrors) throw new ParameterValidationException(errors);

            (collectedErrors ??= []).AddRange(errors);
        }

        if (collectedErrors is not null) throw new ParameterValidationException(collectedErrors);

        await next(context);
    }
}