using System.Reflection;
using Kurisu.AspNetCore.DynamicApi.Attributes;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Kurisu.AspNetCore.DynamicApi.Defaults;

/// <summary>
/// 默认动态api provider
/// </summary>
public class DefaultDynamicApiControllerFeatureProvider : ControllerFeatureProvider
{
    /// <summary>
    /// 判断是否是controller
    /// </summary>
    /// <param name="typeInfo"></param>
    /// <returns></returns>
    protected override bool IsController(TypeInfo typeInfo)
    {
        if (!typeInfo.IsPublic || typeInfo.IsAbstract || typeInfo.IsGenericType)
        {
            return false;
        }

        return typeInfo.GetCustomAttribute<AsApiAttribute>() != null;
    }
}