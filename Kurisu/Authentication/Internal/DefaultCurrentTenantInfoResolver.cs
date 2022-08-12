using System.Security.Claims;
using Kurisu.Authentication.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kurisu.Authentication.Internal
{
    /// <summary>
    /// 租户信息获取处理器
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class DefaultCurrentTenantInfoResolver : ICurrentTenantInfoResolver
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DefaultCurrentTenantInfoResolver(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 定义租户key
        /// </summary>
        /// <remarks>clasims: tenant; header: X-Requested-TenantId</remarks>
        public virtual string TenantKey => "tenant";

        /// <summary>
        /// 获取租户id
        /// </summary>
        /// <returns></returns>
        public virtual int GetTenantId()
        {
            //return _httpContextAccessor.HttpContext.Request.Headers.ContainsKey(TenantKey) ? int.Parse(_httpContextAccessor.HttpContext.Request.Headers[TenantKey]) : 0;
            var tenantValue = _httpContextAccessor.HttpContext.User.FindFirst(TenantKey)?.Value;
            return !string.IsNullOrEmpty(tenantValue) ? int.Parse(tenantValue) : 0;
        }
    }
}