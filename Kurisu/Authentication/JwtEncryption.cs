using System;
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
    /// token生成
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="key"></param>
    /// <param name="iss"></param>
    /// <param name="aud"></param>
    /// <param name="exp"></param>
    /// <returns></returns>
    public static string GenerateToken(string userId, string key, string iss, string aud, int exp = 7200)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var dtNow = DateTime.Now;
        var dtExpires = DateTime.Now.AddSeconds(exp);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new(JwtClaimTypes.Issuer, iss),
                new(JwtClaimTypes.Audience, aud),
                new(JwtClaimTypes.Id, userId),
                new(JwtClaimTypes.NotBefore, dtNow.ToString(CultureInfo.InvariantCulture)),
                new(JwtClaimTypes.Expiration, dtExpires.ToString(CultureInfo.InvariantCulture))
            }),
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