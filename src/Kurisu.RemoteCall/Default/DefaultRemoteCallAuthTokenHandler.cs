using Kurisu.RemoteCall.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Kurisu.RemoteCall.Default;

internal class DefaultRemoteCallAuthTokenHandler : IRemoteCallAuthTokenHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DefaultRemoteCallAuthTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> GetTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext
                          ?? throw new InvalidOperationException("获取HttpContext失败,HttpContext为Null,请检查是否注入IHttpContextAccessor或在请求的作用域中.");

        var token = httpContext.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException($"使用请求的token失败,但HttpContext.Headers[{HeaderNames.Authorization}] 为Null");
        }

        return await Task.FromResult(token);
    }
}