using System;
using Microsoft.AspNetCore.Mvc;

namespace Kurisu.AspNetCore.DynamicApi.Attributes;

/// <summary>
/// 标记为动态API生成
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = false)]
public class DynamicApiAttribute : RouteAttribute
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="template"></param>
    public DynamicApiAttribute(string template) : base(template)
    {
    }
}