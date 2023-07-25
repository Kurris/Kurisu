using System.Linq;
using System.Security.Claims;
using Kurisu.Authentication.Abstractions;
using Mapster;
using Microsoft.AspNetCore.Http;

namespace Kurisu.Authentication.Internal;

/// <summary>
/// 默认当前用户信息处理器
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DefaultCurrentUserInfoResolver : ICurrentUserInfoResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DefaultCurrentUserInfoResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }


    /// <summary>
    /// httpContext
    /// </summary>
    private HttpContext HttpContext => _httpContextAccessor.HttpContext;

    /// <summary>
    /// 获取用户id
    /// </summary>
    /// <returns></returns>
    public virtual T GetSubjectId<T>()
    {
        //MS框架对值token claim的key进行了转换
        var subject = HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(subject) ? default : subject.Adapt<T>();
    }

    /// <summary>
    /// 获取用户请求token
    /// </summary>
    /// <returns></returns>
    public virtual string GetBearerToken()
    {
        var bearerToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        return bearerToken;
    }
}