using Kurisu.Core.Proxy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.SqlSugar.Aops;

/// <summary>
/// sugar aop 基类
/// </summary>
public abstract class BaseSqlSugarAop : Aop
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BaseSqlSugarAop(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 当前请求作用域Db对象
    /// </summary>
    public ISqlSugarClient Db => _httpContextAccessor.HttpContext.RequestServices.GetService<ISqlSugarClient>();

    /// <summary>
    /// http上下文访问器
    /// </summary>
    public IHttpContextAccessor Accessor => _httpContextAccessor;
}
