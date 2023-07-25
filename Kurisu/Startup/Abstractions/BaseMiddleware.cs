using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Kurisu.Startup.Abstractions;

/// <summary>
/// 中间件抽象类
/// </summary>
public abstract class BaseMiddleware
{
    /// <summary>
    /// 管道中下一个方法
    /// </summary>
    protected RequestDelegate Next { get; }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="next"></param>
    // ReSharper disable once PublicConstructorInAbstractClass
    public BaseMiddleware(RequestDelegate next)
    {
        Next = next;
    }

    /// <summary>
    /// 中间件请求处理
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract Task Invoke(HttpContext context);
}