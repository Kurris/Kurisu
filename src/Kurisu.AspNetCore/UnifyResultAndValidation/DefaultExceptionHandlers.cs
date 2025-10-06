using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.UnifyResultAndValidation;
using Kurisu.AspNetCore.UnifyResultAndValidation.Attributes;
using Kurisu.AspNetCore.UnifyResultAndValidation.Exceptions;
using Kurisu.AspNetCore.Utils.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

namespace Kurisu.AspNetCore.UnifyResultAndValidation;

/// <summary>
/// 默认异常处理器，实现框架级异常处理逻辑。
/// </summary>
public class DefaultExceptionHandlers : BaseFrameworkExceptionHandlers
{
    /// <summary>
    /// 统一API结果返回接口。
    /// </summary>
    private readonly IApiResult _apiResult;

    /// <summary>
    /// Http上下文访问器。
    /// </summary>
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数，注入依赖。
    /// </summary>
    /// <param name="apiResult">API结果接口</param>
    /// <param name="httpContextAccessor">Http上下文访问器</param>
    /// <param name="logger"></param>
    public DefaultExceptionHandlers(IApiResult apiResult, IHttpContextAccessor httpContextAccessor, ILogger<DefaultExceptionHandlers> logger)
        : base(logger)
    {
        _apiResult = apiResult;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 统一API结果返回接口（保护属性，便于子类访问）。
    /// </summary>
    protected IApiResult ApiResult => _apiResult;

    /// <summary>
    /// Http上下文访问器（保护属性，便于子类访问）。
    /// </summary>
    protected IHttpContextAccessor HttpContextAccessor => _httpContextAccessor;

    /// <summary>
    /// 处理通用异常。
    /// </summary>
    /// <param name="ex">异常对象</param>
    [HandleException<Exception>]
    public virtual async Task ExceptionHandle(Exception ex)
    {
        var context = _httpContextAccessor.HttpContext!;
        var apiLogSetting = context.RequestServices.GetRequiredService<ApiLogSetting>();
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = ex.Source!.Contains("IdentityModel.AspNetCore") ? 401 : 500;

            // 序列化异常信息为驼峰格式JSON
            var apiResult = _apiResult!.GetDefaultErrorApiResult(ex.Message);
            var responseJson = JsonConvert.SerializeObject(apiResult, JsonExtensions.DefaultSetting);
            var bytes = Encoding.UTF8.GetBytes(responseJson);

            context.Response.ContentType = "application/json";
            context.Response.ContentLength = bytes.Length;
            await context.Response.BodyWriter.WriteAsync(bytes);

            if (apiLogSetting.EnableApiRequestLog)
            {
                apiLogSetting.IsGlobal = true;
                apiLogSetting.Response = apiResult;
                apiLogSetting.Log(false);
            }
        }

        if (apiLogSetting.Title.IsPresent())
        {
            using (LogContext.PushProperty("Prefix", $"[{apiLogSetting.Title}]"))
            {
                Logger.LogError(ex, "未知异常:{error}", ex.Message);
            }
        }
        else
        {
            Logger.LogError(ex, "未知异常:{error}", ex.Message);
        }
    }

    /// <summary>
    /// 处理用户友好异常。
    /// </summary>
    /// <param name="ex">用户友好异常对象</param>
    [HandleException<UserFriendlyException>]
    public virtual void UserFriendlyExceptionHandle(UserFriendlyException ex)
    {
        var context = ex.ExceptionContext;

        var apiResult = context.HttpContext.RequestServices.GetService<IApiResult>()!;
        var errorData = apiResult.GetDefaultErrorApiResult(context.Exception.Message);
        context.Result = new ObjectResult(errorData);

        var setting = context.HttpContext.RequestServices.GetRequiredService<ApiLogSetting>();
        if (setting.EnableApiRequestLog)
        {
            setting.Response = JsonConvert.SerializeObject(errorData);
            setting.Log(false);
        }
    }

    /// <summary>
    /// 处理模型验证异常。
    /// </summary>
    /// <param name="ex">模型验证异常对象</param>
    [HandleException<ModelStateNotValidException>]
    public virtual void ModelStateNotValidExceptionHandle(ModelStateNotValidException ex)
    {
        var context = ex.Context;
        var errorResults = context.ModelState.Keys.SelectMany(key =>
                context.ModelState[key]!.Errors.Where(_ => !string.IsNullOrEmpty(key))
                    .Select(x => new
                    {
                        Field = key,
                        Msg = x.ErrorMessage
                    }))
            .ToList();

        ex.Context.Result = !errorResults.Any()
            ? new ObjectResult(_apiResult.GetDefaultValidateApiResult("None Body Request"))
            : new ObjectResult(_apiResult.GetDefaultValidateApiResult("参数验证失败", errorResults.DistinctBy(x => x.Field)));
        ex.Context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
}