using System.Collections;
using System.Reflection;
using Kurisu.RemoteCall.Abstractions;
using Newtonsoft.Json.Linq;

namespace Kurisu.RemoteCall.Utils;

/// <summary>
/// 注入式通用工具类，包含远程调用相关的辅助方法。
/// 已替换为实例服务以便注入序列化器实现。
/// </summary>
internal class CommonUtils : ICommonUtils
{
    private readonly IJsonSerializer _jsonSerializer;

    public CommonUtils(IJsonSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer;
    }

    public bool IsReferenceType(Type type)
    {
        return (type.IsClass || type.IsInterface || type.IsArray) && type != typeof(string);
    }


    private bool TryParseToObjDictionaryWhenJToken(string prefix, object obj, out Dictionary<string, object> result)
    {
        result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (obj == null)
        {
            return false;
        }

        if (obj is not JToken jToken) return false;

        if (jToken is JValue jValue)
        {
            result[prefix] = jValue.Value;
            return true;
        }

        if (jToken is JObject jObj)
        {
            foreach (var prop in jObj.Properties())
            {
                var combinedKey = CombineKey(prefix, prop.Name);
                foreach (var kv in ToObjDictionary(combinedKey, prop.Value))
                {
                    result[kv.Key] = kv.Value;
                }
            }

            return true;
        }

        if (jToken is JArray jArray)
        {
            var idx = 0;
            foreach (var item in jArray)
            {
                var itemKey = prefix != null ? $"{prefix}[{idx}]" : $"[{idx}]";
                foreach (var kv in ToObjDictionary(itemKey, item))
                {
                    result[kv.Key] = kv.Value;
                }

                idx++;
            }

            if (idx == 0 && prefix != null)
            {
                result[prefix] = null;
            }

            return true;
        }

        return false;
    }


    private bool TryParseToObjDictionaryWhenSimpleType(string prefix, object obj, out Dictionary<string, object> result)
    {
        result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (obj == null)
        {
            return false;
        }

        if (TypeHelper.IsSimpleType(obj.GetType()))
        {
            if (string.IsNullOrEmpty(prefix))
            {
                throw new ArgumentException("Prefix cannot be null or empty for simple types.");
            }

            result[prefix] = obj;
            return true;
        }

        return false;
    }

    private bool TryParseToObjDictionaryWhenNonGenericDict(string prefix, object obj, out Dictionary<string, object> result)
    {
        result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (obj is IDictionary nonGenericDict)
        {
            foreach (DictionaryEntry de in nonGenericDict)
            {
                var keyName = de.Key.ToString();
                var combinedKey = CombineKey(prefix, keyName);
                foreach (var kv in ToObjDictionary(combinedKey, de.Value))
                {
                    result[kv.Key] = kv.Value;
                }
            }

            return true;
        }

        return false;
    }

    private bool TryParseToObjDictionaryWhenGenericDict(string prefix, object obj, out Dictionary<string, object> result)
    {
        result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var objType = obj.GetType();
        var dictInterface = objType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictInterface == null) return false;

        if (obj is not IEnumerable kvEnumerable) return false;

        foreach (var kvp in kvEnumerable)
        {
            var kvpType = kvp.GetType();
            var keyProp = kvpType.GetProperty("Key");
            var valueProp = kvpType.GetProperty("Value");
            var keyObj = keyProp?.GetValue(kvp)?.ToString() ?? string.Empty;
            var valObj = valueProp?.GetValue(kvp);
            var combinedKey = CombineKey(prefix, keyObj);
            foreach (var child in ToObjDictionary(combinedKey, valObj))
            {
                result[child.Key] = child.Value;
            }
        }

        return true;
    }

    private bool TryParseToObjDictionaryWhenEnumerable(string prefix, object obj, out Dictionary<string, object> result)
    {
        result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (obj is not (IEnumerable enumerable and not string)) return false;

        var idx = 0;
        foreach (var item in enumerable)
        {
            var itemKey = prefix != null ? $"{prefix}[{idx}]" : $"[{idx}]";
            foreach (var kv in ToObjDictionary(itemKey, item))
            {
                result[kv.Key] = kv.Value;
            }

            idx++;
        }

        if (idx == 0 && prefix != null)
        {
            result[prefix] = null;
        }

        return true;
    }

    private bool TryParseToObjDictionaryWhenJustObject(string prefix, object obj, out Dictionary<string, object> result)
    {
        result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var serialized = _jsonSerializer.Serialize(obj);
            var d = _jsonSerializer.Deserialize<Dictionary<string, object>>(serialized);
            if (d != null)
            {
                foreach (var kv in d)
                {
                    var combinedKey = CombineKey(prefix, kv.Key);
                    if (kv.Value == null || TypeHelper.IsSimpleType(kv.Value.GetType()))
                    {
                        result[combinedKey] = kv.Value;
                    }
                    else
                    {
                        foreach (var child in ToObjDictionary(combinedKey, kv.Value))
                        {
                            result[child.Key] = child.Value;
                        }
                    }
                }

                return true;
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    public Dictionary<string, object> ToObjDictionary(string prefix, object obj)
    {
        // Handle null explicitly: if obj is null, return a dictionary indicating a null value for the prefix
        if (obj == null)
        {
            var nullResult = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(prefix))
            {
                nullResult[prefix] = null;
            }

            return nullResult;
        }

        if (TryParseToObjDictionaryWhenJToken(prefix, obj, out var result))
        {
            return result;
        }

        if (TryParseToObjDictionaryWhenSimpleType(prefix, obj, out result))
        {
            return result;
        }

        if (TryParseToObjDictionaryWhenNonGenericDict(prefix, obj, out result))
        {
            return result;
        }

        if (TryParseToObjDictionaryWhenGenericDict(prefix, obj, out result))
        {
            return result;
        }

        if (TryParseToObjDictionaryWhenEnumerable(prefix, obj, out result))
        {
            return result;
        }

        if (TryParseToObjDictionaryWhenJustObject(prefix, obj, out result))
        {
            return result;
        }

        var props = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);
        foreach (var p in props)
        {
            var val = p.GetValue(obj);
            var combinedKey = CombineKey(prefix, p.Name);
            foreach (var kv in ToObjDictionary(combinedKey, val))
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
        if (key.StartsWith("["))
            return $"{prefix}{key}";
        return $"{prefix}.{key}";
    }
}