using System;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
using Kurisu.Startup;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kurisu.DataAccessor.Functions.UnitOfWork.Attributes
{
    /// <summary>
    /// 工作单元,局部事务
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class UnitOfWorkAttribute : Attribute, IAsyncActionFilter
    {
        private readonly bool _isAutomaticSaveChanges;

        /// <summary>
        /// 工作单元,非自动提交
        /// </summary>
        public UnitOfWorkAttribute() : this(false)
        {
        }

        /// <summary>
        /// 工作单元
        /// </summary>
        /// <param name="isAutomaticSaveChanges">是否自动提交</param>
        public UnitOfWorkAttribute(bool isAutomaticSaveChanges)
        {
            _isAutomaticSaveChanges = isAutomaticSaveChanges;
        }

        /// <summary>
        /// 当事务保存成功后,接受数据库的更改
        /// </summary>
        public bool AcceptAllChangesOnSuccess { get; set; } = true;

        /// <summary>
        /// 接口过滤器
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var dbContextContainer = context.HttpContext.RequestServices.GetService<IDbContextContainer>();
            if (dbContextContainer != null)
            {
                //is not
                if (context.HttpContext.RequestServices.GetService(typeof(IUnitOfWorkDbContext)) is not Func<IServiceProvider, DbContext> unitOfWorkDbContextResolver)
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<UnitOfWorkAttribute>>();
                    logger.LogWarning("启用{UnitOfWork}但是无法处理unitOfWorkDbContextResolver,结果 {Result}", nameof(UnitOfWork), null);
                    await next();
                }
                else
                {
                    dbContextContainer.Manage(unitOfWorkDbContextResolver.Invoke(context.HttpContext.RequestServices));
                    dbContextContainer.IsAutomaticSaveChanges = _isAutomaticSaveChanges;

                    //开启事务
                    await dbContextContainer.BeginTransactionAsync();

                    //获取结果
                    var result = await next();

                    //提交事务
                    await dbContextContainer.CommitTransactionAsync(AcceptAllChangesOnSuccess, result.Exception);
                }
            }
            else
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<UnitOfWorkAttribute>>();
                logger.LogWarning("找不到服务: {Service},工作单元{UnitOfWork}失效,请在{ConfigureServices}使用:{Method}进行服务注册"
                    , nameof(IDbContextContainer)
                    , nameof(UnitOfWork)
                    , nameof(DefaultKurisuStartup.ConfigureServices)
                    , nameof(UnitOfWorkServiceCollectionExtensions.AddKurisuUnitOfWork));

                await next();
            }
        }
    }
}