using Kurisu.Extensions.FluentValidation.Abstractions;
using System.Reflection;

namespace Kurisu.Extensions.FluentValidation.Internal;

/// <summary>
/// 参数的验证位置和名称, 统一代理筛选与闭合方法的类型判断.
/// </summary>
internal readonly record struct ValidationParameter(int Position, string Name)
{
    public static bool RequiresValidation(ParameterInfo parameter)
    {
        if (parameter.IsOut) return false;
        var type = GetParameterType(parameter);
        // 开放泛型保留代理入口, 实际调用时再判断闭合类型.
        return type.IsGenericParameter || IsModelType(type);
    }

    public static ValidationParameter? Create(ParameterInfo declaration, ParameterInfo implementation)
    {
        if (declaration.IsOut) return null;

        var type = GetParameterType(declaration);
        if (type.ContainsGenericParameters) type = GetParameterType(implementation);
        var name = declaration.Name ?? $"arg{declaration.Position}";
        if (type.ContainsGenericParameters)
            throw new InvalidOperationException($"无法解析参数 {name} 的闭合验证类型.");

        return IsModelType(type) ? new ValidationParameter(declaration.Position, name) : null;
    }

    private static Type GetParameterType(ParameterInfo parameter)
        => parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;

    private static bool IsModelType(Type type) => typeof(IAopValidatableObject).IsAssignableFrom(type);
}