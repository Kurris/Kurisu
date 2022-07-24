using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor.UnitOfWork.Attributes
{
    /// <summary>
    /// 设置是否自动提交,默认为false
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AutomaticSaveChangesAttribute : Attribute, IAsyncActionFilter
    {
        private readonly bool _isAutomaticSaveChanges;

        public AutomaticSaveChangesAttribute(bool isAutomaticSaveChanges)
        {
            _isAutomaticSaveChanges = isAutomaticSaveChanges;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var masterDb = context.HttpContext.RequestServices.GetService<IAppMasterDb>();
            if (masterDb != null)
            {
                if (masterDb.GetMasterDbContext() is IAppDbContext dbContext)
                {
                    dbContext.IsAutomaticSaveChanges = _isAutomaticSaveChanges;
                }
            }

            await next();
        }
    }
}
