using System.Collections;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kurisu.RemoteCall.Utils;

/// <summary>
/// 通用类，包含远程调用相关的辅助方法。
/// </summary>
public static class CommonUtils
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

    /// <summary>
    /// 将对象展开为键值字典，支持复杂对象、泛型/非泛型 IDictionary、IEnumerable、嵌套结构。
    /// 如果提供了 pKey，则作为顶层前缀使用（会与子属性用 '.' 连接或集合用 '[index]' 表示）。
    /// </summary>
    /// <param name="obj">输入对象，可能为 null、简单类型、复杂类型、集合或字典。</param>
    /// <param name="pKey">可选的键名前缀（null 表示无前缀）。</param>
    /// <returns>扁平化的键->值字典（值可能为 null 或原始对象）。</returns>
    public static Dictionary<string, object> ToObjDictionary(this object obj, string pKey)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (obj == null) return result;

        // Handle JSON.NET JToken types (JValue, JObject, JArray)
        if (obj is JToken jtoken)
        {
            if (jtoken is JValue jvalue)
            {
                var key = pKey;
                result[key] = jvalue.Value;
                return result;
            }

            if (jtoken is JObject jobj)
            {
                foreach (var prop in jobj.Properties())
                {
                    var combinedKey = CombineKey(pKey, prop.Name);
                    foreach (var kv in ToObjDictionary(prop.Value, combinedKey))
                    {
                        result[kv.Key] = kv.Value;
                    }
                }

                return result;
            }

            if (jtoken is JArray jarr)
            {
                var idx = 0;
                foreach (var item in jarr)
                {
                    var itemKey = pKey != null ? $"{pKey}[{idx}]" : $"[{idx}]";
                    foreach (var kv in ToObjDictionary(item, itemKey))
                    {
                        result[kv.Key] = kv.Value;
                    }

                    idx++;
                }

                if (idx == 0 && pKey != null)
                {
                    result[pKey] = null;
                }

                return result;
            }
        }

        // 如果是简单类型（包含 string、enum、primitive、DateTime、Guid 等），直接返回单项（使用 pKey）
        if (TypeHelper.IsSimpleType(obj.GetType()))
        {
            var key = pKey;
            result[key] = obj;
            return result;
        }

        // 非泛型 IDictionary
        if (obj is IDictionary nonGenericDict)
        {
            foreach (DictionaryEntry de in nonGenericDict)
            {
                var keyName = de.Key.ToString();
                var combinedKey = CombineKey(pKey, keyName);
                foreach (var kv in ToObjDictionary(de.Value, combinedKey))
                {
                    result[kv.Key] = kv.Value;
                }
            }

            return result;
        }

        // 泛型 IDictionary<,>
        var objType = obj.GetType();
        var dictInterface = objType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictInterface != null)
        {
            // 通过 IEnumerable 枚举 KeyValuePair<,>
            if (obj is IEnumerable kvEnumerable)
            {
                foreach (var kvp in kvEnumerable)
                {
                    // 尝试通过反射读取 Key/Value
                    var kvpType = kvp.GetType();
                    var keyProp = kvpType.GetProperty("Key");
                    var valueProp = kvpType.GetProperty("Value");
                    var keyObj = keyProp?.GetValue(kvp)?.ToString() ?? string.Empty;
                    var valObj = valueProp?.GetValue(kvp);
                    var combinedKey = CombineKey(pKey, keyObj);
                    foreach (var child in ToObjDictionary(valObj, combinedKey))
                    {
                        result[child.Key] = child.Value;
                    }
                }
            }

            return result;
        }

        // IEnumerable（但排除 string），将按索引展开
        if (obj is IEnumerable enumerable && !(obj is string))
        {
            var idx = 0;
            foreach (var item in enumerable)
            {
                var itemKey = pKey != null ? $"{pKey}[{idx}]" : $"[{idx}]";
                foreach (var kv in ToObjDictionary(item, itemKey))
                {
                    result[kv.Key] = kv.Value;
                }

                idx++;
            }

            // 如果集合为空且有前缀，保留空键用于表示存在
            if (idx == 0 && pKey != null)
            {
                result[pKey] = null;
            }

            return result;
        }

        // 尝试通过 JSON 序列化/反序列化成字典（处理匿名对象等）
        try
        {
            var serialized = JsonConvert.SerializeObject(obj);
            var d = JsonConvert.DeserializeObject<Dictionary<string, object>>(serialized);
            if (d != null)
            {
                foreach (var kv in d)
                {
                    var combinedKey = CombineKey(pKey, kv.Key);
                    // 若 value 为简单类型，直接写入；若为复杂对象，递归展开
                    if (kv.Value == null || TypeHelper.IsSimpleType(kv.Value.GetType()))
                    {
                        result[combinedKey] = kv.Value;
                    }
                    else
                    {
                        foreach (var child in ToObjDictionary(kv.Value, combinedKey))
                        {
                            result[child.Key] = child.Value;
                        }
                    }
                }

                return result;
            }
        }
        catch
        {
            // ignore json fallback 异常，转入反射分支
        }

        // 反射取属性并递归展开
        var props = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);
        foreach (var p in props)
        {
            var val = p.GetValue(obj);
            var combinedKey = CombineKey(pKey, p.Name);
            foreach (var kv in ToObjDictionary(val, combinedKey))
            {
                result[kv.Key] = kv.Value;
            }
        }

        return result;
    }


    private static string CombineKey(string prefix, string key)
    {
        if (string.IsNullOrEmpty(prefix)) return key ?? string.Empty;
        if (string.IsNullOrEmpty(key)) return prefix;
        // 如果 key 已经以 '[' 开头（表示索引），直接拼接，否则用点号分层
        if (key.StartsWith("["))
            return $"{prefix}{key}";
        return $"{prefix}.{key}";
    }
}