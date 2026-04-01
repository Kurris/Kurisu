using System;
using Kurisu.AspNetCore.DynamicApi;
using Kurisu.AspNetCore.DynamicApi.Defaults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Kurisu.AspNetCore.Startup;

/// <summary>
/// 启动项配置
/// </summary>
public class StartupOptions
{
    /// <summary>
    /// ctor
    /// </summary>
    public StartupOptions()
    {


        RouteOptions = options => options.LowercaseUrls = true;

        MvcOptions = options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

        DynamicApiOptions = new DynamicApiOptions
        {
            ModelConvention = new DefaultDynamicApiConvention(),
            ControllerFeatureProvider = new DefaultDynamicApiControllerFeatureProvider()
        };
    }

    public string LanguageHeaderName { get; set; }

    /// <summary>
    /// 路由配置
    /// </summary>
    /// <remarks>
    /// 默认 LowercaseUrls = true
    /// </remarks>
    public Action<RouteOptions> RouteOptions { get; set; }

    /// <summary>
    /// mvc配置
    /// </summary>
    /// <remarks>
    /// 默认: SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true
    /// </remarks>
    public Action<MvcOptions> MvcOptions { get; set; }

    // /// <summary>
    // /// mvc newtonsoft.json 配置
    // /// </summary>
    //public Action<MvcNewtonsoftJsonOptions> MvcNewtonsoftJsonOptions { get; set; }

    /// <summary>
    /// 动态api配置
    /// </summary>
    public DynamicApiOptions DynamicApiOptions { get; set; }
}