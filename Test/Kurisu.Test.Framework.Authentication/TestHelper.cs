using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Kurisu.AspNetCore.Authentication.Abstractions;
using Kurisu.AspNetCore.Authentication.Defaults;
using Microsoft.AspNetCore.Http;

namespace Kurisu.Test.Framework.Authentication;

public class TestHelper
{
    /// <summary>
    /// 获取用户信息处理器
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static ICurrentUser GetResolver(string token)
    {
        var jwtSecurityToken = new JwtSecurityToken(token);

        var claims = new HashSet<Claim>();

        foreach (var claim in jwtSecurityToken.Claims)
        {
            claims.Add(JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.TryGetValue(claim.Type, out string value)
                ? new Claim(value, claim.Value)
                : claim);
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

        var httpContext = new DefaultHttpContext();
        // var type = Assembly.Load("Microsoft.AspNetCore.Http").GetType("Microsoft.AspNetCore.Http.DefaultHttpRequest");
        // _ = Activator.CreateInstance(type!, httpContext);

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        httpContextAccessor.HttpContext.Request.Headers.Add("Authorization", "Bearer " + token);
        httpContextAccessor.HttpContext.User = principal;

        return new DefaultCurrentUser(httpContextAccessor);
    }
}