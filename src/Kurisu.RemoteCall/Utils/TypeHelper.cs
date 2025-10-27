// csharp

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

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            if (type.IsGenericType)
            {
                elementType = type.GetGenericArguments().FirstOrDefault() ?? typeof(object);
            }
            else
            {
                elementType = typeof(object);
            }

            return true;
        }

        return false;
    }

    public static bool IsEnumerableOfSimple(Type type)
    {
        if (!IsEnumerable(type, out var et)) return false;
        return IsSimpleType(et);
    }
}