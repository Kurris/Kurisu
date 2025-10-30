using System.Collections;

namespace Kurisu.RemoteCall.Utils;

internal static class TypeHelper
{
    public static bool IsSimpleType(Type type)
    {
        if (type == null) return false;
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsPrimitive || type.IsEnum) return true;

        if (type == typeof(string)
            || type == typeof(int) 
            || type == typeof(uint) 
            || type == typeof(long) 
            || type == typeof(ulong)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(Uri)
            || type == typeof(byte[]))
        {
            return true;
        }

        return false;
    }

    // 改进：优先识别 IEnumerable<T> 接口并返回具体元素类型
    public static bool IsEnumerable(Type type, out Type elementType)
    {
        elementType = null;
        if (type == null) return false;

        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return true;
        }

        if (type == typeof(string)) return false; // string 不是集合处理对象

        // 优先查找 IEnumerable<T>
        var genericIEnum = type.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (genericIEnum != null)
        {
            elementType = genericIEnum.GetGenericArguments().FirstOrDefault() ?? typeof(object);
            return true;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            elementType = typeof(object);
            return true;
        }

        return false;
    }

    public static bool IsEnumerableOfSimple(Type type)
    {
        if (!IsEnumerable(type, out var et)) return false;
        return IsSimpleType(et);
    }

    public static bool IsDictionary(Type type, out Type keyType, out Type valueType)
    {
        keyType = null;
        valueType = null;
        if (type == null) return false;

        var genericDict = type.GetInterfaces()
            .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (genericDict != null)
        {
            var args = genericDict.GetGenericArguments();
            keyType = args[0];
            valueType = args[1];
            return true;
        }

        if (typeof(IDictionary).IsAssignableFrom(type))
        {
            keyType = typeof(object);
            valueType = typeof(object);
            return true;
        }

        return false;
    }

   
    public static bool IsComplexType(Type type)
    {
        if (type == null) return false;
        var t = Nullable.GetUnderlyingType(type) ?? type;

        if (IsSimpleType(t)) return false;
        if (t == typeof(string)) return false;

        // 类或非原始的值类型（struct）视为复杂类型
        if (t.IsClass) return true;
        if (t.IsValueType && !t.IsPrimitive && !t.IsEnum) return true;

        return false;
    }

    public static bool IsReferenceType(Type type)
    {
        if (type == null) return false;
        type = Nullable.GetUnderlyingType(type) ?? type;
        return (type.IsClass || type.IsInterface || type.IsArray) && type != typeof(string);
    }
}