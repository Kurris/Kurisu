using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
    /// <param name="userClaims">用户信息</param>
    /// <param name="key">密钥</param>
    /// <param name="iss">颁发</param>
    /// <param name="aud">受众</param>
    /// <param name="exp">过期时间(sec)</param>
    /// <returns></returns>
    public static string GenerateToken(IEnumerable<Claim> userClaims, string key, string iss, string aud, int exp = 7200)
    {
        if (userClaims == null) throw new ArgumentNullException(nameof(userClaims));
        if (key.Length < 15) throw new ArgumentException(nameof(key) + "长度低于15位");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var dtNow = DateTime.UtcNow;
        var dtExpires = dtNow.AddSeconds(exp);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(userClaims),
            Issuer = iss,
            Audience = aud,
            IssuedAt = dtNow, //颁发时间
            NotBefore = dtNow,
            Expires = dtExpires,
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        };

        var jwtHandler = new JwtSecurityTokenHandler();
        var securityToken = jwtHandler.CreateToken(descriptor);
        var jwtToken = jwtHandler.WriteToken(securityToken);

        return jwtToken;
    }
}