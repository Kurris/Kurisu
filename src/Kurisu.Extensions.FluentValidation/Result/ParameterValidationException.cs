using Kurisu.AspNetCore.Abstractions.Result;

namespace Kurisu.Extensions.FluentValidation.Result;

/// <summary>
/// 参数验证失败, 交给宿主的框架异常处理扩展输出字段错误.
/// </summary>
public sealed class ParameterValidationException(IReadOnlyList<ParameterValidationError> errors)
    : UserFriendlyException("参数验证失败")
{
    public IReadOnlyList<ParameterValidationError> Errors { get; } = errors ?? throw new ArgumentNullException(nameof(errors));
}