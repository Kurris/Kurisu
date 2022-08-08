using System.Security.Claims;
using Kurisu.Authentication.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kurisu.Authentication.Internal
{
    /// <summary>
    /// 默认当前用户信息处理器
    /// </summary>
    internal class DefaultCurrentUserInfoResolver : ICurrentUserInfoResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultCurrentUserInfoResolver(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取用户id
        /// </summary>
        /// <returns></returns>
        public int GetSubjectId()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var subject = (httpContext?.User.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(subject, out int sub)
                ? sub
                : 0;
        }
    }
}