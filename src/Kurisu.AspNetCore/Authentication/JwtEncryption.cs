using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityModel;
using Microsoft.IdentityModel.Tokens;

namespace Kurisu.AspNetCore.Authentication;

/// <summary>
/// jwt
/// </summary>
public static class JwtEncryption
{
    /// <summary>
    /// 生成token
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <param name="claims">额外信息</param>
    /// <param name="key">密钥</param>
    /// <param name="iss">颁发</param>
    /// <param name="aud">受众</param>
    /// <param name="exp">过期时间(sec)</param>
    /// <returns></returns>
    public static string GenerateToken(IEnumerable<Claim> user, IEnumerable<Claim> claims, string key, string iss, string aud, int exp = 7200)
    {
        if (key.Length < 15)
        {
            key = key.PadRight(15 + 1, '0');
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var dtNow = DateTime.Now;
        var dtExpires = DateTime.Now.AddSeconds(exp);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(),
            Claims = new Dictionary<string, object>
            {
                [JwtClaimTypes.Issuer] = iss,
                [JwtClaimTypes.Audience] = aud,
                [JwtClaimTypes.NotBefore] = $"{new DateTimeOffset(dtNow).ToUnixTimeSeconds()}",
                [JwtClaimTypes.Expiration] = $"{new DateTimeOffset(dtExpires).ToUnixTimeSeconds()}"
            },
            Issuer = iss,
            IssuedAt = dtNow, //颁发时间
            NotBefore = dtNow,
            Expires = dtExpires,
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        };

        descriptor.Subject.AddClaims(user);
        foreach (var item in claims)
        {
            descriptor.Claims.Add(item.Type, item.Value);
        }

        var jwtHandler = new JwtSecurityTokenHandler();
        var securityToken = jwtHandler.CreateToken(descriptor);
        var jwtToken = jwtHandler.WriteToken(securityToken);

        return jwtToken;
    }
}