// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Text;
// using Kurisu.RemoteCall.Abstractions;
// using Microsoft.IdentityModel.Tokens;
//
// namespace Kurisu.Test.WebApi_A
// {
//     public class JipTokenHandler : IAuthTokenHandler
//     {
//         public string GetToken(IServiceProvider serviceProvider)
//         {
//             var configuration = serviceProvider.GetService<IConfiguration>();
//             return "Bearer " + IssueToken(configuration.GetSection("JinkoOptions:Jip:Key").Value, configuration.GetSection("JinkoOptions:Jip:Secret").Value);
//         }
//
//
//         public string IssueToken(string jwtKey, string jwtSecret)
//         {
//             // var jwtKey = key;//_configuration.GetSection("JinkoOptions:Jip:Key").Value;
//             //var jwtSecret = secret;// _configuration.GetSection("JinkoOptions:Jip:Secret").Value;
//             var mins = 5;
//
//             var now = DateTime.Now.AddMinutes(-mins);
//             var expires = DateTime.Now.AddMinutes(mins);
//
//             var claims = new List<Claim>
//         {
//             new (JwtRegisteredClaimNames.Nbf,$"{new DateTimeOffset(now).ToUnixTimeSeconds()}"),
//             new (JwtRegisteredClaimNames.Exp,$"{new DateTimeOffset(expires).ToUnixTimeSeconds()}"),
//             new (JwtRegisteredClaimNames.Iss,jwtKey),
//         };
//
//             var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
//             var descriptor = new SecurityTokenDescriptor
//             {
//                 Subject = new ClaimsIdentity(claims),
//                 Issuer = jwtKey,
//                 IssuedAt = now,
//                 NotBefore = now,
//                 Expires = expires,
//                 SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
//             };
//
//             var jwtHandler = new JwtSecurityTokenHandler();
//             var securityToken = jwtHandler.CreateToken(descriptor);
//             return jwtHandler.WriteToken(securityToken);
//         }
//     }
// }
