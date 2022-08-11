using System.Linq;
using System.Security.Claims;
using Kurisu.Authentication.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kurisu.Authentication.Internal
{
    /// <summary>
    /// 默认当前用户信息处理器
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class DefaultCurrentUserInfoResolver : ICurrentUserInfoResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultCurrentUserInfoResolver(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        /// <summary>
        /// httpContext
        /// </summary>
        private HttpContext HttpContext => _httpContextAccessor.HttpContext;

        /// <summary>
        /// 获取用户id
        /// </summary>
        /// <returns></returns>
        public virtual int GetSubjectId()
        {
            var subject = HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(subject, out int sub)
                ? sub
                : 0;
        }

        /// <summary>
        /// 获取用户请求token
        /// </summary>
        /// <returns></returns>
        public virtual string GetBearerToken()
        {
            var bearerToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            return bearerToken;
        }
    }
}