using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Kurisu.Authorization.Attributes
{
    public class JAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _group;
        private readonly string _jAuth;

        public JAuthorizeAttribute(string group, string jAuth)
        {
            _group = group;
            _jAuth = jAuth;
        }


        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            throw new NotImplementedException();
        }
    }
}