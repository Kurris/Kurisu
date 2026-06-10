using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

namespace Kurisu.Test.DataAccess.Filter.Mock;

/// <summary>
/// 默认从方法第一个 string 参数解析租户ID。
/// </summary>
public class MethodArgumentTenantResolver : IUseTenantResolver
{
    /// <inheritdoc />
    public ValueTask<string> ResolveTenantIdAsync(UseTenantResolveContext context)
    {
        if (context.Arguments is not { Length: > 0 })
        {
            throw new InvalidOperationException("未能解析当前租户ID：方法没有参数。请确保方法至少包含一个 string 类型参数作为租户ID。");
        }

        if (context.Arguments[0] is not string tenantId)
        {
            throw new InvalidOperationException(
                $"未能解析当前租户ID：方法第一个参数类型为 {context.Arguments[0]?.GetType().FullName ?? "null"}，期望为 string 类型。");
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("未能解析当前租户ID：方法第一个 string 参数值为空。");
        }

        return new ValueTask<string>(tenantId);
    }
}
