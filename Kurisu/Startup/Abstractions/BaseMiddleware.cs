using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Kurisu.Startup.Abstractions
{
    /// <summary>
    /// 中间件抽象类
    /// </summary>
    public abstract class BaseMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="next"></param>
        // ReSharper disable once PublicConstructorInAbstractClass
        public BaseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// 中间件请求处理
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract Task Invoke(HttpContext context);
    }
}