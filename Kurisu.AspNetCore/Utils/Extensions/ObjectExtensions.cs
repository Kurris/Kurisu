namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// 对象扩展
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// 存在
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool IsPresent<T>(this T obj) where T : class
    {
        return !IsEmpty(obj);
    }

    /// <summary>
    /// 空值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool IsEmpty<T>(this T obj) where T : class
    {
        if (obj is string str)
        {
            return str.IsEmpty();
        }

        return obj == null;
    }
}
