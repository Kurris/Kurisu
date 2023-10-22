using System;
using System.Linq;
using System.Security.Claims;
using Kurisu.Authentication.Abstractions;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Kurisu.Authentication.Defaults;

/// <summary>
/// 默认当前用户信息处理器
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DefaultCurrentUserInfoResolver : ICurrentUserInfo
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DefaultCurrentUserInfoResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 请求HttpContext
    /// </summary>
    protected HttpContext HttpContext => _httpContextAccessor.HttpContext;

    /// <summary>
    /// 获取用户id
    /// </summary>
    /// <returns></returns>
    public virtual T GetSubjectId<T>()
    {
        //microsoft identity model框架对值token claim的key进行了转换
        var subject = HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(subject) ? default : subject.Adapt<T>();
    }

    /// <summary>
    /// 获取用户请求token
    /// </summary>
    /// <returns></returns>
    public virtual string GetBearerToken()
    {
        var bearerToken = HttpContext.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
        return bearerToken;
    }

    public Guid GetUidSubjectId() => GetSubjectId<Guid>();

    public string GetStringSubjectId() => GetSubjectId<string>();

    public int GetIntSubjectId() => GetSubjectId<int>();

}