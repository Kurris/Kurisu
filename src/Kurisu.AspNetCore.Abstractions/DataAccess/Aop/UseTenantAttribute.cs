using System.Reflection;
using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

/// <summary>
/// 指定当前数据库操作使用的租户。
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public class UseTenantAttribute : AopAttribute
{
    private protected readonly string _parameterName;

    /// <summary>
    /// 默认取方法第一个参数作为租户ID，参数必须为 string 类型。
    /// </summary>
    public UseTenantAttribute()
    {
    }

    /// <summary>
    /// 从方法参数或参数对象属性解析租户。
    /// </summary>
    /// <param name="parameterName">参数名或参数对象属性名。</param>
    public UseTenantAttribute(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName)) throw new ArgumentException("参数名不能为空", nameof(parameterName));
        _parameterName = parameterName;
    }

    /// <summary>
    /// 指定当前数据库操作租户。
    /// </summary>
    /// <param name="context">切面上下文。</param>
    /// <param name="next">下一个切面委托。</param>
    /// <returns></returns>
    public override async Task Invoke(AspectContext context, AspectDelegate next)
    {
        var tenantId = await ResolveTenantIdAsync(context);
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("未能解析当前租户ID，请确认 UseTenant 已配置有效的租户解析方式");
        }

        var dbContext = context.ServiceProvider.GetRequiredService<IDbContext>();
        using (dbContext.UseTenant(tenantId))
        {
            await next(context);
        }
    }

    /// <summary>
    /// 解析租户ID。当指定了 <c>parameterName</c> 时从方法参数解析，
    /// 否则取方法第一个参数作为租户ID（必须为 string 类型）。
    /// </summary>
    /// <param name="context">切面上下文。</param>
    /// <returns>租户ID。</returns>
    protected virtual ValueTask<string> ResolveTenantIdAsync(AspectContext context)
    {
        if (!string.IsNullOrWhiteSpace(_parameterName))
        {
            return new ValueTask<string>(ResolveFromParameter(context, _parameterName));
        }

        // 无 parameterName 时，取方法第一个参数作为租户ID
        if (context.Parameters is not { Length: > 0 })
        {
            throw new InvalidOperationException("UseTenant 未指定参数名且方法没有参数，无法解析租户ID。请指定参数名或确保方法第一个参数为 string 类型的租户ID。");
        }

        if (context.Parameters[0] is not string tenantId)
        {
            throw new InvalidOperationException(
                $"UseTenant 默认取方法第一个参数作为租户ID，但第一个参数类型为 {context.Parameters[0]?.GetType().FullName ?? "null"}，期望为 string 类型。");
        }

        return new ValueTask<string>(tenantId);
    }

    /// <summary>
    /// 从方法参数或参数对象属性解析租户，支持 JSON 路径语法（如 <c>"input.TenantId"</c>）。
    /// </summary>
    /// <param name="context">切面上下文。</param>
    /// <param name="name">参数名或 JSON 路径（如 <c>"paramName"</c>、<c>"paramName.Property"</c>、<c>"paramName.Nested.Value"</c>）。</param>
    /// <returns>租户ID。</returns>
    protected static string ResolveFromParameter(AspectContext context, string name)
    {
        // 解析点号路径：第一个点号之前为参数名，之后为属性路径
        var dotIndex = name.IndexOf('.');
        var paramName = dotIndex > 0 ? name.Substring(0, dotIndex) : name;
        var propertyPath = dotIndex > 0 ? name.Substring(dotIndex + 1) : null;

        var parameters = context.ImplementationMethod.GetParameters();
        for (var i = 0; i < parameters.Length && i < context.Parameters.Length; i++)
        {
            if (!string.Equals(parameters[i].Name, paramName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = context.Parameters[i];

            // 无属性路径时直接返回参数值的字符串表示
            if (propertyPath == null)
            {
                return value?.ToString();
            }

            // 从参数对象沿属性路径导航取值
            return ResolveFromPropertyPath(value, propertyPath);
        }

        return null;
    }

    /// <summary>
    /// 沿点号分隔的属性路径从对象中取值。
    /// </summary>
    /// <param name="value">起始对象。</param>
    /// <param name="propertyPath">点号分隔的属性路径（如 <c>"TenantId"</c> 或 <c>"Nested.Value"</c>）。</param>
    /// <returns>属性值的字符串表示。</returns>
    protected static string ResolveFromPropertyPath(object value, string propertyPath)
    {
        if (value == null) return null;
        if (string.IsNullOrWhiteSpace(propertyPath)) return value.ToString();

        var segments = propertyPath.Split('.');
        var current = value;

        foreach (var segment in segments)
        {
            if (current == null) return null;

            var property = current.GetType().GetProperty(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null) return null;

            current = property.GetValue(current);
        }

        return current?.ToString();
    }
}

/// <summary>
/// 使用指定 <see cref="IUseTenantResolver"/> 类型解析租户，Resolver 直接通过无参构造函数创建。
/// </summary>
/// <typeparam name="TResolver">租户解析器类型，必须实现 <see cref="IUseTenantResolver"/> 且具有无参构造函数。</typeparam>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public class UseTenantAttribute<TResolver> : UseTenantAttribute where TResolver : IUseTenantResolver, new()
{
    /// <summary>
    /// 使用指定 <see cref="IUseTenantResolver"/> 类型解析租户。
    /// </summary>
    public UseTenantAttribute()
    {
    }

    /// <summary>
    /// 优先从方法参数解析租户，解析不到时再使用指定 Resolver。
    /// </summary>
    /// <param name="parameterName">参数名或参数对象属性名。</param>
    public UseTenantAttribute(string parameterName) : base(parameterName)
    {
    }

    /// <inheritdoc />
    protected override ValueTask<string> ResolveTenantIdAsync(AspectContext context)
    {
        if (!string.IsNullOrWhiteSpace(_parameterName))
        {
            return new ValueTask<string>(ResolveFromParameter(context, _parameterName));
        }

        var resolver = new TResolver();
        var resolveContext = new UseTenantResolveContext(context.ServiceProvider, context.ImplementationMethod, context.Parameters);
        return resolver.ResolveTenantIdAsync(resolveContext);
    }
}
