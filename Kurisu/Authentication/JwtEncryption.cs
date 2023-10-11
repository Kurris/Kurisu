using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityModel;
using Microsoft.IdentityModel.Tokens;

namespace Kurisu.Authentication;

public class JwtEncryption
{
    /// <summary>
    /// 生成token
    /// </summary>
    /// <param name="user"></param>
    /// <param name="claims"></param>
    /// <param name="key"></param>
    /// <param name="iss"></param>
    /// <param name="aud"></param>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static string GenerateToken(Claim[] user, Claim[] claims, string key, string iss, string aud, int exp = 7200)
    {
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
                [JwtClaimTypes.NotBefore] = dtNow.ToString(CultureInfo.InvariantCulture),
                [JwtClaimTypes.Expiration] = dtExpires.ToString(CultureInfo.InvariantCulture),
            },
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