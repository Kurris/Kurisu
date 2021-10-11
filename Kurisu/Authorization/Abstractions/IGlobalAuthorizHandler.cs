using Microsoft.AspNetCore.Mvc.Filters;

namespace Kurisu.Authorization.Abstractions
{
    public interface IGlobalAuthorizHandler
    {
        /// <summary>
        /// 鉴权完成后
        /// </summary>
        /// <param name="context"></param>
        void OnAuthorization(AuthorizationFilterContext context);
    }
}