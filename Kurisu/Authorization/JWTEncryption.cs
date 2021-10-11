using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityModel;
using Microsoft.IdentityModel.Tokens;

namespace Kurisu.Authorization
{
    /// <summary>
    /// jwt 加密
    /// </summary>
    public static class JWTEncryption
    {
        /// <summary>
        /// token 生成方法
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="key"></param>
        /// <param name="exp"></param>
        /// <param name="iss"></param>
        /// <param name="aud"></param>
        /// <returns></returns>
        public static string GenerateToken(string userId, string key, int exp, string iss, string aud)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            DateTime dtNow = DateTime.Now;
            DateTime dtExpires = DateTime.Now.AddMinutes(exp);

            var descriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(JwtClaimTypes.Issuer, iss),
                    new Claim(JwtClaimTypes.Audience, aud),
                    new Claim(JwtClaimTypes.Id, userId),
                    new Claim(JwtClaimTypes.NotBefore, dtNow.ToString()),
                    new Claim(JwtClaimTypes.Expiration, dtExpires.ToString())
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
}