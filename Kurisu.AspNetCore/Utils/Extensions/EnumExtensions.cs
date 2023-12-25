using System;
using System.ComponentModel;
using System.Reflection;

namespace Kurisu.AspNetCore.Utils.Extensions;

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
        if (f!.IsDefined(typeof(DescriptionAttribute), false))
        {
            return f.GetCustomAttribute<DescriptionAttribute>()!.Description;
        }
        return value.ToString();
    }


    public static string GetDescription(this Type value)
    {
        if (value!.IsDefined(typeof(DescriptionAttribute), false))
        {
            return value.GetCustomAttribute<DescriptionAttribute>()!.Description;
        }
        return value.Name;
    }
}
