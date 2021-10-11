using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kurisu.Authorization.Attributes
{
    public class AppAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        public AppAuthorizeAttribute(params string[] policies)
        {
            if (policies is { Length: > 0 })
            {
                Policies = policies;
            }
        }

        /// <summary>
        /// 策略
        /// </summary>
        private string[] Policies
        {
            get => Policy?.Split(",", StringSplitOptions.RemoveEmptyEntries);
            set => Policy = string.Join(",", value);
        }


        public void OnAuthorization(AuthorizationFilterContext context)
        {
            
        }
    }
}