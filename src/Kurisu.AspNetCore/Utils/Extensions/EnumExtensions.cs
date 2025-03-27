using System;
using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore.CustomClass;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// 枚举扩展
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// 获取<see cref="LangAttribute"/>描述
    /// </summary>
    /// <param name="value"></param>
    /// <param name="lang"></param>
    /// <returns></returns>
    public static string GetDisplay(this Enum value, string lang = "cn")
    {
        var f = value.GetType().GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static)!;
        return f.IsDefined(typeof(LangAttribute))
            ? f.GetCustomAttributes<LangAttribute>().FirstOrDefault(x => x.Lang == lang)?.Display
            : value.ToString();
    }

    /// <summary>
    /// 获取Description
    /// </summary>
    /// <param name="value"></param>
    /// <param name="lang"></param>
    /// <returns></returns>
    public static string GetDisplay(this Type value, string lang = "cn")
    {
        return value.IsDefined(typeof(LangAttribute))
            ? value.GetCustomAttributes<LangAttribute>().FirstOrDefault(x => x.Lang == lang)?.Display
            : value.Name;
    }
}