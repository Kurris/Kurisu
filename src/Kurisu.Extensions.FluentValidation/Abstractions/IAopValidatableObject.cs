using FluentValidation;

namespace Kurisu.Extensions.FluentValidation.Abstractions;

/// <summary>
/// 由参数模型定义自己的验证规则, 异步执行交给扩展的方法拦截器.
/// </summary>
public interface IAopValidatableObject
{
    /// <summary>
    /// 创建当前模型的验证器, 规则可以使用当前调用作用域中的服务.
    /// </summary>
    /// <param name="serviceProvider">当前调用作用域的服务提供者.</param>
    /// <returns>适用于当前模型的 FluentValidation 验证器.</returns>
    IValidator CreateValidator(IServiceProvider serviceProvider);
}