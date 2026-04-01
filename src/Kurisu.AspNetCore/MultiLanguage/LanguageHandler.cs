using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
using Kurisu.AspNetCore.Abstractions.Utils.Extensions;
using Kurisu.AspNetCore.Utils.Extensions;
using Newtonsoft.Json;

namespace Kurisu.AspNetCore.MultiLanguage;

/// <summary>
/// 多语言处理器
/// </summary>
public class LanguageHandler
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<string, List<string>>> LangGroupCache = new();


    /// <summary>
    /// 处理结果
    /// </summary>
    /// <param name="language"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static object HandleResult(string language, object value)
    {
        if (value == null) return null;

        var type = value.GetType();
        // basic exclusions
        if (!type.IsClass || type == typeof(string)) return value;

        // handle dictionaries by processing their values (preserve keys)
        if (typeof(IDictionary).IsAssignableFrom(type))
        {
            var idict = (IDictionary)value;
            var resultDict = new Dictionary<object, object>(idict.Count);
            foreach (DictionaryEntry de in idict)
            {
                resultDict[de.Key] = HandleResult(language, de.Value);
            }

            return resultDict;
        }

        // if it's an enumerable, determine element type (if available) to decide whether to process
        if (IsEnumerable(type))
        {
            var elementType = GetEnumerableElementType(type);
            if (elementType != null)
            {
                if (!elementType.IsClass || elementType == typeof(string)) return value;
            }

            return HandleListType(language, value);
        }

        return HandleClassType(language, value);
    }

    /// <summary>
    /// 处理list
    /// </summary>
    /// <param name="language"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private static IEnumerable HandleListType(string language, object value)
    {
        if (value == null) return null;

        var result = new List<object>();
        var list = value as IEnumerable;

        foreach (var item in list!)
        {
            var type = item.GetType();
            if (!type.IsClass || type == typeof(string)) continue;

            result.Add(
                IsEnumerable(type)
                    ? HandleListType(language, item)
                    : HandleClassType(language, item)
            );
        }

        return result;
    }

    /// <summary>
    /// 处理object
    /// </summary>
    /// <param name="language"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private static ExpandoObject HandleClassType(string language, object value)
    {
        if (value == null) return null;

        var type = value.GetType();
        var properties = PropertyCache.GetOrAdd(type, t => t.GetProperties());

        // use a plain dictionary to build the result and avoid JObject overhead
        var dict = new Dictionary<string, object>(properties.Length);
        var propertyNames = new List<string>(properties.Length);
        var seen = new HashSet<string>(properties.Length);

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
            {
                continue;
            }

            var name = property.Name;
            if (!seen.Add(name)) continue; // ensure unique by exact name

            var v = property.GetValue(value);
            v = HandleResult(language, v);

            propertyNames.Add(name);
            dict[name] = v;
        }

        // process multi-language groups: use cached groups per Type to avoid O(n^2) work
        var groups = LangGroupCache.GetOrAdd(type, t =>
        {
            var names = properties.Select(p => p.Name).ToList();
            var map = new Dictionary<string, List<string>>(names.Count);
            foreach (var name in names)
            {
                var list = names.Where(x => x == name || (x.StartsWith(name) && x.Length > name.Length && x.Substring(name.Length).All(char.IsLetter))).ToList();
                map[name] = list;
            }

            return map;
        });

        foreach (var propertyName in propertyNames)
        {
            if (!dict.ContainsKey(propertyName)) continue;

            if (!groups.TryGetValue(propertyName, out var langNames) || langNames.Count <= 1) continue;

            // default to simplified Chinese (keep base)
            if (language.IsEmpty() || language.Equals("zh", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var otherLangName in langNames.Where(x => x != propertyName))
                {
                    dict.Remove(otherLangName);
                }
            }
            else
            {
                var currentLangName = propertyNames.FirstOrDefault(x => x.Equals(propertyName + language, StringComparison.OrdinalIgnoreCase));
                if (currentLangName.IsPresent())
                {
                    var languageValue = dict.GetValueOrDefault(currentLangName);
                    if (languageValue is string s && s.IsPresent())
                    {
                        dict[propertyName] = s;
                    }
                }

                foreach (var otherLangName in langNames.Where(x => x != propertyName))
                {
                    dict.Remove(otherLangName);
                }
            }
        }

        // convert dictionary to ExpandoObject
        var expando = new ExpandoObject();
        var expandoDict = (IDictionary<string, object>)expando;
        foreach (var kv in dict)
        {
            expandoDict[kv.Key] = kv.Value;
        }

        return expando;
    }

    /// <summary>
    /// 判断是否为IsEnumerable
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsEnumerable(Type type)
    {
        if (type == null) return false;

        if (type.IsArray)
        {
            return true;
        }

        if (type == typeof(string)) return false; // string 不是集合处理对象

        // 优先查找 IEnumerable<T>
        var genericIEnum = type.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (genericIEnum != null)
        {
            return true;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Try to get the element type for an IEnumerable{T} or array. Returns null if unknown.
    /// </summary>
    private static Type GetEnumerableElementType(Type type)
    {
        if (type.IsArray) return type.GetElementType();

        // find IEnumerable<T>
        var genericIEnum = type.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (genericIEnum != null)
        {
            return genericIEnum.GetGenericArguments()[0];
        }

        return null;
    }
}