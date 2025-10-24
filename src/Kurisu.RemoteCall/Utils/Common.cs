using System.Collections;
using System.Reflection;
using Newtonsoft.Json;

namespace Kurisu.RemoteCall.Utils;

/// <summary>
/// 通用类，包含远程调用相关的辅助方法。
/// </summary>
internal static class Common
{
    /// <summary>
    /// 判断指定类型是否为引用类型（排除字符串类型）。
    /// </summary>
    /// <param name="type">要判断的类型。</param>
    /// <returns>如果是引用类型且不是字符串，则返回 true；否则返回 false。</returns>
    internal static bool IsReferenceType(Type type)
    {
        // 接口、类、数组都是引用类型
        return (type.IsClass || type.IsInterface || type.IsArray) && type != typeof(string);
    }

    public static Dictionary<string, object> ToObjDictionary(this object obj)
    {
        if (obj == null) return new Dictionary<string, object>();

        // 如果是集合（除了 IDictionary），不支持表单转换，返回空字典
        if (obj is IEnumerable and not IDictionary)
        {
            return new Dictionary<string, object>();
        }

        // 支持非泛型 IDictionary
        if (obj is IDictionary dict)
        {
            var result = new Dictionary<string, object>();
            foreach (DictionaryEntry de in dict)
            {
                var key = de.Key.ToString()!;
                result[key] = de.Value;
            }

            return result;
        }

        // 尝试通过 JSON 序列化/反序列化为字典
        try
        {
            var d = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(obj));
            if (d != null) return d;
        }
        catch
        {
            // ignore
        }

        // 反射取属性
        var dictResult = new Dictionary<string, object>();
        var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var p in props)
        {
            var v = p.GetValue(obj);
            dictResult[p.Name] = v;
        }

        return dictResult;
    }

    public static Dictionary<string, string> ToStringDictionary(this object dict)
    {
        var objDictionary = dict.ToObjDictionary();
        return objDictionary.ToDictionary(x => x.Key, x => x.Value?.ToString() ?? string.Empty);
    }
}