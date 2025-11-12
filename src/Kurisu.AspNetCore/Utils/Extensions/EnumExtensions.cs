using System;
using System.ComponentModel;
using System.Reflection;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// 枚举扩展类，提供用于枚举和类型的扩展方法以获取描述信息。
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 获取枚举值的<see cref="DescriptionAttribute"/>描述。
    /// 如果未定义<see cref="DescriptionAttribute"/>，则返回枚举值的名称。
    /// </summary>
    /// <param name="value">枚举值。</param>
    /// <returns>枚举值的描述或名称。</returns>
    public static string GetDescription(this Enum value)
    {
        var f = value.GetType().GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static)!;
        return f.GetCustomAttribute<DescriptionAttribute>() is { } attr ? attr.Description : value.ToString();
    }

    /// <summary>
    /// 获取类型的<see cref="DescriptionAttribute"/>描述。
    /// 如果未定义<see cref="DescriptionAttribute"/>，则返回类型的名称。
    /// </summary>
    /// <param name="value">类型对象。</param>
    /// <returns>类型的描述或名称。</returns>
    public static string GetDescription(this Type value)
    {
        return value.GetCustomAttribute<DescriptionAttribute>() is { } attr ? attr.Description : value.Name;
    }
}