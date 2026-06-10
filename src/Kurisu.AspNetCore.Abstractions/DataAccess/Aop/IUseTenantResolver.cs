using System.Reflection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Aop;

/// <summary>
/// UseTenant 租户解析器。
/// </summary>
public interface IUseTenantResolver
{
    /// <summary>
    /// 解析当前数据库操作使用的租户ID。
    /// </summary>
    /// <param name="context">租户解析上下文。</param>
    /// <returns>租户ID。</returns>
    ValueTask<string> ResolveTenantIdAsync(UseTenantResolveContext context);
}

/// <summary>
/// UseTenant 租户解析上下文。
/// </summary>
public sealed class UseTenantResolveContext
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="serviceProvider">服务提供器。</param>
    /// <param name="method">被拦截的方法。</param>
    /// <param name="arguments">方法参数值。</param>
    public UseTenantResolveContext(IServiceProvider serviceProvider, MethodInfo method, object[] arguments)
    {
        ServiceProvider = serviceProvider;
        Method = method;
        Arguments = arguments ?? [];

        var parameters = method.GetParameters();
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < parameters.Length && i < Arguments.Length; i++)
        {
            values[parameters[i].Name!] = Arguments[i];
        }

        ParameterValues = values;
    }

    /// <summary>
    /// 服务提供器。
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 被拦截的方法。
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// 方法参数值。
    /// </summary>
    public object[] Arguments { get; }

    /// <summary>
    /// 按参数名索引的方法参数值。
    /// </summary>
    public IReadOnlyDictionary<string, object> ParameterValues { get; }
}
