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
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new ArgumentNullException(nameof(_httpContextAccessor.HttpContext), "HttpContext is null when attempting to get authorization token. Skipping Authorization header.");
        }

        var token = httpContext.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentNullException(nameof(token), "Authorization header is missing in the current HttpContext. Skipping Authorization header.");
        }

        return await Task.FromResult(token);
    }
}