using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.UnitOfWork.Attributes;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kurisu.DataAccessor.UnitOfWork.Filters
{
    public class UnitOfWorkFilter : IAsyncActionFilter, IOrderedFilter
    {
        /// <summary>
        /// 上下文容器
        /// </summary>
        private readonly IDbContextContainer _dbContextContainer;

        public UnitOfWorkFilter(IDbContextContainer dbContextContainer)
        {
            _dbContextContainer = dbContextContainer;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var actionDescriptor = (ControllerActionDescriptor) context.ActionDescriptor;
            var method = actionDescriptor.MethodInfo;

            //定义工作单元
            if (!method.IsDefined(typeof(UnitOfWorkAttribute), true))
            {
                var result = await next();

                //没有异常则提交事务
                if (result.Exception == null)
                {
                    await _dbContextContainer.SaveChangesAsync();
                }
            }
            else
            {
                //开启事务
                await _dbContextContainer.BeginTransactionAsync();
                //获取api结果
                var result = await next();
                //提交事务
                await _dbContextContainer.CommitTransactionAsync(result.Exception);
            }

            await _dbContextContainer.CloseAsync();
        }

        public int Order => 9999;
    }
}