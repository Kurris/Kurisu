using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Kurisu.AspNetCore.Abstractions.Authentication;
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
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    public DefaultCurrentUser(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
    {
    }

    /// <summary>
    /// 获取用户id
    /// </summary>
    /// <returns></returns>
    public virtual int GetUserId()
    {
        //microsoft identity model框架对值token claim.key进行了转换
        return HttpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value.Adapt<int>() ?? -1;
    }

    /// <summary>
    /// 获取用户id
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public virtual T GetUserId<T>()
    {
        var userId = GetUserId();
        return userId.Adapt<T>();
    }

    /// <summary>
    /// 获取用户请求token
    /// </summary>
    /// <returns></returns>
    public virtual string GetAccessToken()
    {
        return HttpContextAccessor.HttpContext?.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
    }

    /// <summary>
    /// 获取user name
    /// </summary>
    /// <returns></returns>
    public string GetName(string userClaimType = "name")
    {
        var name = GetUserClaim(userClaimType);
        return name;
    }

    /// <summary>
    /// 获取user claim
    /// </summary>
    /// <param name="claimType"></param>
    /// <returns></returns>
    public string GetUserClaim(string claimType)
    {
        var value = HttpContextAccessor
            .HttpContext?
            .User.Claims.FirstOrDefault(x => x.Type == claimType)?.Value;

        return value;
    }


    /// <summary>
    /// 获取所有角色
    /// </summary>
    /// <returns>所有角色集合，如无则为空集合</returns>
    public IEnumerable<string> GetRoles()
    {
        return HttpContextAccessor.HttpContext?
                   .User.FindAll(ClaimTypes.Role)
                   .Select(c => c.Value)
               ?? [];
    }
}