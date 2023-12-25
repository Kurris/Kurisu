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
    /// 请求HttpContext
    /// </summary>
    protected HttpContext HttpContext => _httpContextAccessor.HttpContext;

    private string _subject = string.Empty;

    /// <summary>
    /// 获取用户id
    /// </summary>
    /// <returns></returns>
    public virtual T GetSubjectId<T>()
    {
        if (string.IsNullOrEmpty(_subject))
        {
            //microsoft identity model框架对值token claim的key进行了转换
            _subject = HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        return string.IsNullOrEmpty(_subject) ? default : _subject.Adapt<T>();
    }

    /// <summary>
    /// 获取用户请求token
    /// </summary>
    /// <returns></returns>
    public virtual string GetToken()
    {
        var bearerToken = HttpContext.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
        return bearerToken;
    }

    public Guid GetUidSubjectId() => GetSubjectId<Guid>();

    public string GetStringSubjectId() => GetSubjectId<string>();

    public int GetIntSubjectId() => GetSubjectId<int>();

    public string GetName()
    {
        throw new NotImplementedException();
    }
}