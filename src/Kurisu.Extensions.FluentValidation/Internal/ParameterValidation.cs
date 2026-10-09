using Kurisu.Extensions.FluentValidation.Abstractions;
using Kurisu.Extensions.FluentValidation.Result;
using FluentValidation;

namespace Kurisu.Extensions.FluentValidation.Internal;

/// <summary>
/// 执行验证计划选中的模型规则, 不重复扫描方法元数据或取消令牌.
/// </summary>
internal static class ParameterValidation
{
    public static async ValueTask<ParameterValidationError[]> ValidateAsync<T>(IServiceProvider serviceProvider,
        string parameterName, T value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // 空模型仍按声明类型触发验证, 避免 null 绕过接口约定.
        if (value is null)
            return [new ParameterValidationError(parameterName, parameterName, "参数不能为空")];

        var model = (IAopValidatableObject)value;
        var validator = ModelValidation.ResolveValidator(model, serviceProvider, parameterName);
        var result = await validator.ValidateAsync(new ValidationContext<T>(value), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (result.IsValid) return [];

        var errors = new ParameterValidationError[result.Errors.Count];
        for (var i = 0; i < errors.Length; i++)
        {
            var error = result.Errors[i];
            errors[i] = new ParameterValidationError(parameterName, error.PropertyName, error.ErrorMessage);
        }

        return errors;
    }
}