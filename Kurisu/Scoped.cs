using System;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Abstractions.Operation;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu
{
    /// <summary>
    /// 局部作用域
    /// </summary>
    public sealed class Scoped
    {
        /// <summary>
        /// 创建作用域范围
        /// </summary>
        /// <param name="func">异步方法</param>
        public static async Task CreateAsync(Func<IServiceProvider, Task> func)
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                await func.Invoke(scope.ServiceProvider);
            }
        }

        /// <summary>
        /// 创建作用域范围
        /// </summary>
        /// <param name="action">同步方法</param>
        public static void Create(Action<IServiceProvider> action)
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                action.Invoke(scope.ServiceProvider);
            }
        }

        /// <summary>
        /// 创建作用域范围,带有返回值
        /// </summary>
        /// <param name="func">同步方法</param>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <returns></returns>
        public static TResult Create<TResult>(Func<IServiceProvider, TResult> func)
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                return func.Invoke(scope.ServiceProvider);
            }
        }


        /// <summary>
        /// 创建作用域范围,带有返回值
        /// </summary>
        /// <param name="func">异步方法</param>
        /// <typeparam name="TResult">返回值类型</typeparam>
        /// <returns></returns>
        public static async Task<TResult> CreateAsync<TResult>(Func<IServiceProvider, Task<TResult>> func)
        {
            using (var scope = App.ServiceProvider.CreateScope())
            {
                return await func.Invoke(scope.ServiceProvider);
            }
        }

        /// <summary>
        /// 创建工作单元
        /// </summary>
        /// <param name="func"></param>
        /// <exception cref="Exception"></exception>
        public static async Task CreateUnitOfWorkAsync(Func<IAppDbService, Task> func)
        {
            await CreateAsync(async provider =>
            {
                var dbService = provider.GetService<IAppDbService>();
                await dbService.UseTransactionAsync(async () => await func.Invoke(dbService));
            });
        }
    }
}