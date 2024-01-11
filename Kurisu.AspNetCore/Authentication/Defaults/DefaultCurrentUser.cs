using System;
using System.Linq;
using System.Security.Claims;
using Kurisu.Core.User.Abstractions;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Kurisu.AspNetCore.Authentication.Defaults;

/// <summary>
/// 默认当前用户信息处理器
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DefaultCurrentUser : DefaultCurrentTenant, ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DefaultCurrentUser(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 获取用户id
    /// </summary>
    /// <returns></returns>
    public virtual T GetSubjectId<T>()
    {
        //microsoft identity model框架对值token claim的key进行了转换
        var subject = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrEmpty(subject) ? default : subject.Adapt<T>();
    }

    /// <summary>
    /// 获取用户请求token
    /// </summary>
    /// <returns></returns>
    public virtual string GetToken()
    {
        var token = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
        return token;
    }

    public Guid GetUidSubjectId() => GetSubjectId<Guid>();

    public string GetStringSubjectId() => GetSubjectId<string>();

    public int GetIntSubjectId() => GetSubjectId<int>();

    public string GetName()
    {
        var name = GetUserClaim("preferred_username");
        return name;
    }

    public string GetUserClaim(string claimType)
    {
        var value = _httpContextAccessor?
            .HttpContext?
            .User?.Claims?
            .FirstOrDefault(x => x.Type == claimType)?
            .Value;

        return value;
    }
}