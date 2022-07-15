using System;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
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
        /// <summary>
        /// 是否为手动保存更改
        /// </summary>
        private readonly bool _isManualSaveChanges;

        public UnitOfWorkAttribute() : this(false)
        {
        }

        public UnitOfWorkAttribute(bool isManualSaveChanges)
        {
            _isManualSaveChanges = isManualSaveChanges;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var dbContextContainer = context.HttpContext.RequestServices.GetService<IDbContextContainer>();

                //开启事务
            await dbContextContainer?.BeginTransactionAsync();

            //获取结果
            var result = await next();

            //提交事务
            await dbContextContainer?.CommitTransactionAsync(this._isManualSaveChanges, result.Exception);

        }
    }
}