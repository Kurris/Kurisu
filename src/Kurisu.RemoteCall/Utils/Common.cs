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

   
    internal static async Task UseAuthAsync(this IProxyInvocation invocation, HttpRequestMessage request)
    {
        var auth = invocation.GetCustomAttribute<RequestAuthorizeAttribute>();
        if (auth == null)
        {
            return;
        }

        var headerName = auth.HeaderName;
        string token;
        if (auth.Handler.IsImplementFrom<IRemoteCallAuthTokenHandler>())
        {
            var handler = (IRemoteCallAuthTokenHandler)Activator.CreateInstance(auth.Handler)
                          ?? throw new NullReferenceException($"无法创建 {auth.Handler.FullName} 的实例。");

            token = await handler.GetTokenAsync(invocation.ServiceProvider).ConfigureAwait(false);
            request.Headers.TryAddWithoutValidation(headerName, token);
            return;
        }

        var httpContext = invocation.ServiceProvider.GetService<IHttpContextAccessor>()?.HttpContext
                          ?? throw new NullReferenceException("获取HttpContext失败,HttpContext为Null,请检查是否注入IHttpContextAccessor或在请求的作用域中.");

        token = httpContext.Request.Headers[headerName].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            throw new NullReferenceException($"使用请求的token失败,但HttpContext.Headers[{headerName}] 为Null");
        }

        request.Headers.TryAddWithoutValidation(headerName, token);
    }

    /// <summary>
    /// 判断当前请求是否需要输出日志。
    /// </summary>
    /// <param name="invocation">代理调用信息。</param>
    /// <returns>如果需要输出日志，则返回 true；否则返回 false。</returns>
    internal static bool UseLog(this IProxyInvocation invocation) => invocation.GetCustomAttribute<RequestLogAttribute>() != null;
}