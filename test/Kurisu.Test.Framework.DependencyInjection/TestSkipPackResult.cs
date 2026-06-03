using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Result;
using Kurisu.AspNetCore.UnifyResultAndValidation;
using Kurisu.AspNetCore.UnifyResultAndValidation.Attributes;
using Kurisu.AspNetCore.UnifyResultAndValidation.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

public class TestSkipPackResult
{
    [Fact]
    public async Task ResultHandle_DefaultAction_PacksObjectResult()
    {
        var originalResult = new object();
        var context = CreateContext(nameof(TestController.Default), new ObjectResult(originalResult));

        await ExecuteAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var apiResult = Assert.IsAssignableFrom<IApiResult>(result.Value);
        Assert.Same(originalResult, apiResult.GetData());
    }

    [Fact]
    public async Task ResultHandle_SkipPackResultAction_KeepsObjectResult()
    {
        var originalValue = new object();
        var originalResult = new ObjectResult(originalValue);
        var context = CreateContext(nameof(TestController.Raw), originalResult);

        await ExecuteAsync(context);

        Assert.Same(originalResult, context.Result);
        Assert.Same(originalValue, Assert.IsType<ObjectResult>(context.Result).Value);
    }

    [Fact]
    public async Task ResultHandle_SkipPackResultController_KeepsObjectResult()
    {
        var originalResult = new ObjectResult(new object());
        var context = CreateContext(nameof(RawController.Raw), originalResult, typeof(RawController));

        await ExecuteAsync(context);

        Assert.Same(originalResult, context.Result);
    }

    [Fact]
    public async Task ResultHandle_SkipPackResultAction_KeepsEmptyResult()
    {
        var originalResult = new EmptyResult();
        var context = CreateContext(nameof(TestController.Raw), originalResult);

        await ExecuteAsync(context);

        Assert.Same(originalResult, context.Result);
    }

    [Fact]
    public async Task ResultHandle_SkipPackResultAction_StillPacksValidationError()
    {
        var context = CreateContext(nameof(TestController.Raw), new ObjectResult(new object()));
        context.ModelState.AddModelError("Name", "Name is required");

        await ExecuteAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        var apiResult = Assert.IsAssignableFrom<IApiResult>(result.Value);
        Assert.Equal(ApiStateCode.ValidateError, Assert.IsType<ApiResult<object>>(apiResult).Code);
        Assert.Equal(StatusCodes.Status400BadRequest, context.HttpContext.Response.StatusCode);
    }

    private static ResultExecutingContext CreateContext(string actionName, IActionResult result, System.Type controllerType = null)
    {
        controllerType ??= typeof(TestController);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddScoped<ApiLogSetting>();
        services.AddSingleton<IApiResult, ApiResult<object>>();
        services.AddSingleton<IFrameworkExceptionHandlers, DefaultExceptionHandlers>();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType.GetTypeInfo(),
            MethodInfo = controllerType.GetMethod(actionName)!
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor, new ModelStateDictionary());

        return new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), result, new object());
    }

    private static Task ExecuteAsync(ResultExecutingContext context)
    {
        var filter = new ValidateAndPackResultFilter();
        return filter.OnResultExecutionAsync(context, () => Task.FromResult(
            new ResultExecutedContext(context, new List<IFilterMetadata>(), context.Result, new object())));
    }

    private class TestController
    {
        public void Default()
        {
        }

        [SkipPackResult]
        public void Raw()
        {
        }
    }

    [SkipPackResult]
    private class RawController
    {
        public void Raw()
        {
        }
    }
}
