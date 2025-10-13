using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Utils;

/// <summary>
/// 通用类，包含远程调用相关的辅助方法。
/// </summary>
internal static class Common
{
    /// <summary>
    /// 判断指定类型是否为引用类型（排除字符串类型）。
    /// </summary>
    /// <param name="type">要判断的类型。</param>
    /// <returns>如果是引用类型且不是字符串，则返回 true；否则返回 false。</returns>
    internal static bool IsReferenceType(Type type)
    {
        // 接口、类、数组都是引用类型
        return (type.IsClass || type.IsInterface || type.IsArray) && type != typeof(string);
    }

    /// <summary>
    /// 判断当前请求是否需要授权，并获取授权头及 token。
    /// </summary>
    /// <param name="invocation">代理调用信息。</param>
    /// <returns>
    /// 三元组：
    /// <list type="bullet">
    /// <item><description>是否需要授权。</description></item>
    /// <item><description>授权头名称。</description></item>
    /// <item><description>token 值。</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="NullReferenceException">
    /// 当未能获取到 HttpContext 或请求头中未包含 token 时抛出。
    /// </exception>
    internal static async Task<(bool, string, string)> UseAuthAsync(this IProxyInvocation invocation)
    {
        var auth = invocation.GetCustomAttribute<RequestAuthorizeAttribute>();
        if (auth == null)
        {
            return (false, string.Empty, string.Empty);
        }

        var headerName = auth.HeaderName;
        string token;
        if (auth.Handler.IsImplementFrom<IRemoteCallAuthTokenHandler>())
        {
            var handler = (IRemoteCallAuthTokenHandler?)Activator.CreateInstance(auth.Handler) 
                ?? throw new NullReferenceException($"无法创建 {auth.Handler.FullName} 的实例。");

            token = await handler.GetTokenAsync(invocation.ServiceProvider);
            return (true, headerName, token);
        }

        var httpContext = invocation.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext
                          ?? throw new NullReferenceException("获取HttpContext失败,HttpContext为Null,请检查是否注入IHttpContextAccessor或在请求的作用域中.");

        token = httpContext.Request.Headers[headerName].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            throw new NullReferenceException($"使用请求的token失败, HttpContext Header[{headerName}] 为Null");
        }

        return (true, headerName, token);
    }

    /// <summary>
    /// 判断当前请求是否需要输出日志。
    /// </summary>
    /// <param name="invocation">代理调用信息。</param>
    /// <returns>如果需要输出日志，则返回 true；否则返回 false。</returns>
    internal static bool UseLog(this IProxyInvocation invocation) => invocation.GetCustomAttribute<RequestLogAttribute>() != null;
}