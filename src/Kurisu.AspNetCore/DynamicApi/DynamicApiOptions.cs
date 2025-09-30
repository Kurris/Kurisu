using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Kurisu.AspNetCore.DynamicApi;

/// <summary>
/// 动态api配置
/// </summary>
public class DynamicApiOptions
{
    /// <summary>
    /// 定义api controller接口数据
    /// </summary>
    public IApplicationModelConvention ModelConvention { get; set; }

    /// <summary>
    /// 定义api controller
    /// </summary>
    public ControllerFeatureProvider ControllerFeatureProvider { get; set; }
}