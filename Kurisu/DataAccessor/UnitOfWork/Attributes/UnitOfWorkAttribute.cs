using System;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor.UnitOfWork.Attributes
{
    /// <summary>
    /// 工作单元,局部事务
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class UnitOfWorkAttribute : Attribute, IAsyncActionFilter
    {
        private readonly bool _isAutomaticSaveChanges;

        public UnitOfWorkAttribute() : this(false)
        {
        }

        public UnitOfWorkAttribute(bool isAutomaticSaveChanges)
        {
            _isAutomaticSaveChanges = isAutomaticSaveChanges;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var dbContextContainer = context.HttpContext.RequestServices.GetService<IDbContextContainer>();
            if (dbContextContainer.Count > 0)
            {
                dbContextContainer.IsAutomaticSaveChanges = _isAutomaticSaveChanges;

                //开启事务
                await dbContextContainer.BeginTransactionAsync();

                //获取结果
                var result = await next();

                //提交事务
                await dbContextContainer.CommitTransactionAsync(result.Exception);
            }
            else
            {
                await next();
            }
        }
    }
}