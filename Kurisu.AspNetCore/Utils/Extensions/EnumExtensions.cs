using System;
using System.ComponentModel;
using System.Reflection;
using Kurisu.AspNetCore.CustomClass;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// 枚举扩展
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 获取<see cref="DescriptionAttribute"/>描述
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetDescription(this Enum value)
    {
        var t = value.GetType();
        var f = t.GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
        return f!.IsDefined(typeof(DescriptionAttribute), false)
            ? f.GetCustomAttribute<DescriptionAttribute>()!.Description
            : value.ToString();
    }

    /// <summary>
    /// 获取<see cref="DescriptionEnAttribute"/>描述
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetDescriptionEn(this Enum value)
    {
        var t = value.GetType();
        var f = t.GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
        return f!.IsDefined(typeof(DescriptionEnAttribute), false)
            ? f.GetCustomAttribute<DescriptionEnAttribute>()!.Description
            : value.ToString();
    }

    /// <summary>
    /// 获取Description
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetDescription(this Type value)
    {
        return value!.IsDefined(typeof(DescriptionAttribute), false) 
            ? value.GetCustomAttribute<DescriptionAttribute>()!.Description 
            : value.Name;
    }


    /// <summary>
    /// 获取Description
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetDescriptionEn(this Type value)
    {
        return value!.IsDefined(typeof(DescriptionEnAttribute), false) 
            ? value.GetCustomAttribute<DescriptionEnAttribute>()!.Description 
            : value.Name;
    }
}