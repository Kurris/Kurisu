using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Kurisu.AspNetCore.Authentication.Abstractions;
using Kurisu.AspNetCore.Authentication.Defaults;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Kurisu.Test.Framework.CurrentUserResolver;

[Trait("currentUserInfo", "default")]
public class TestCurrentUserInfo
{
    [Fact]
    public void GetSubject_Return_3()
    {
        var token = "eyJhbGciOiJSUzI1NiIsImtpZCI6IkVCODQxRDM2RTA1OUVBQzFGMDY3RkQzQjJCNjkyMEQ3IiwidHlwIjoiYXQrand0In0.eyJuYmYiOjE2NjAwMjY0MzQsImV4cCI6MTY2MDAzMDAzNCwiaXNzIjoiaWRlbnRpdHkuaXNhd2Vzb21lLmNuIiwiYXVkIjoid2VhdGhlciIsImNsaWVudF9pZCI6InNwYSIsInN1YiI6IjMiLCJhdXRoX3RpbWUiOjE2NjAwMjY0MjEsImlkcCI6ImxvY2FsIiwianRpIjoiQUU5MDI4NzEyNTFDRERFQjFDNThEQ0Q3M0UyMTlEQzMiLCJzaWQiOiJDRkQwNzkyMTU4MzJGNjdBQjZFNTkzMzEyQjgwMDMyMyIsImlhdCI6MTY2MDAyNjQzNCwic2NvcGUiOlsib3BlbmlkIiwicHJvZmlsZSIsIndlYXRoZXI6c2VhcmNoIiwib2ZmbGluZV9hY2Nlc3MiXSwiYW1yIjpbInB3ZCJdfQ.S3q77NoIG0ZoU9EoRg7tKUvDKwAFnfkelEgh8bCaLjAxCXYV0NGiK_2nTfwC10WgjZnXPw0ZXT9ZF_oPKZMEOAghICKnYLAfOhlS8nQIaVuBxqZEzodXhp-VlZ04KJUeWyux0DXqZsrvv7gXitIU_IknncZWvNHO4zHrV7YgLqOrrR6nBVK3M6eGfQ7KTsJg9smON2izMCTb6vFbTSzIMDVrdJiymyokaQkAKolU0-kIRbWyI8ilSjZrnjvOomw_q78hCfBVBk0W4Tyf1M9mXHfwfpOIswlozfWjDm85zvR4HK7hechTogaPzMjIFmIMOMn_rqZJKVlIhqXB1cFvww";

        var sub = GetResolver(token).GetSubjectId<int>();

        Assert.Equal(3, sub);
    }


    [Fact]
    public void GetBearerToken()
    {
        var token = "eyJhbGciOiJSUzI1NiIsImtpZCI6IkVCODQxRDM2RTA1OUVBQzFGMDY3RkQzQjJCNjkyMEQ3IiwidHlwIjoiYXQrand0In0.eyJuYmYiOjE2NjAwMjY0MzQsImV4cCI6MTY2MDAzMDAzNCwiaXNzIjoiaWRlbnRpdHkuaXNhd2Vzb21lLmNuIiwiYXVkIjoid2VhdGhlciIsImNsaWVudF9pZCI6InNwYSIsInN1YiI6IjMiLCJhdXRoX3RpbWUiOjE2NjAwMjY0MjEsImlkcCI6ImxvY2FsIiwianRpIjoiQUU5MDI4NzEyNTFDRERFQjFDNThEQ0Q3M0UyMTlEQzMiLCJzaWQiOiJDRkQwNzkyMTU4MzJGNjdBQjZFNTkzMzEyQjgwMDMyMyIsImlhdCI6MTY2MDAyNjQzNCwic2NvcGUiOlsib3BlbmlkIiwicHJvZmlsZSIsIndlYXRoZXI6c2VhcmNoIiwib2ZmbGluZV9hY2Nlc3MiXSwiYW1yIjpbInB3ZCJdfQ.S3q77NoIG0ZoU9EoRg7tKUvDKwAFnfkelEgh8bCaLjAxCXYV0NGiK_2nTfwC10WgjZnXPw0ZXT9ZF_oPKZMEOAghICKnYLAfOhlS8nQIaVuBxqZEzodXhp-VlZ04KJUeWyux0DXqZsrvv7gXitIU_IknncZWvNHO4zHrV7YgLqOrrR6nBVK3M6eGfQ7KTsJg9smON2izMCTb6vFbTSzIMDVrdJiymyokaQkAKolU0-kIRbWyI8ilSjZrnjvOomw_q78hCfBVBk0W4Tyf1M9mXHfwfpOIswlozfWjDm85zvR4HK7hechTogaPzMjIFmIMOMn_rqZJKVlIhqXB1cFvww";

        var accessToken = GetResolver(token).GetToken();

        Assert.Equal("Bearer " + token, accessToken);
    }


    /// <summary>
    /// 获取用户信息处理器
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private static ICurrentUser GetResolver(string token)
    {
        var jwtSecurityToken = new JwtSecurityToken(token);

        var claims = new HashSet<Claim>();

        foreach (var claim in jwtSecurityToken.Claims)
        {
            claims.Add(JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.ContainsKey(claim.Type)
                ? new Claim(JwtSecurityTokenHandler.DefaultInboundClaimTypeMap[claim.Type], claim.Value)
                : claim);
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

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