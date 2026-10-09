namespace Kurisu.Extensions.FluentValidation.Result;

/// <summary>
/// 验证器返回的参数名称、字段和错误消息.
/// </summary>
public sealed record ParameterValidationError(string Parameter, string Field, string Message);