using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kurisu.EFSharding.Extensions;

internal static class TypeExtension
{
    /// <summary>
    /// 是否为可空类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsNullableType(this Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

    /// <summary>
    /// 是否为可比较类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsComparableType(this Type type) => type.IsAssignableTo(typeof(IComparable));

    /// <summary>
    /// 检测是否是数字类型,包括nullable的数字类型
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <remarks>
    /// bool 不是数字类型
    /// </remarks>
    public static bool IsNumericType(this Type type)
    {
        if (type == null) return false;

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.SByte:
            case TypeCode.Single:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return true;
            case TypeCode.Object:
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return IsNumericType(Nullable.GetUnderlyingType(type));

                return false;
            case TypeCode.Empty:
            case TypeCode.DBNull:
            case TypeCode.Boolean:
            case TypeCode.Char:
            case TypeCode.DateTime:
            case TypeCode.String:
            default:
                return false;
        }
    }


    /// <summary>
    /// 是否是简单类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsSimpleType(this Type type)
    {
        return type.IsPrimitive || type.IsValueType || type == typeof(string);
    }

    /// <summary>
    /// 是否是bool类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsBooleanType(Type type)
    {
        if (type == null) return false;
        return Type.GetTypeCode(type) == TypeCode.Boolean;
    }

    /// <summary>
    /// 匿名类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool IsAnonymousType(this Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        // HACK: The only way to detect anonymous types right now.
        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
               && type.IsGenericType && type.Name.Contains("AnonymousType")
               && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
               && type.Attributes.HasFlag(TypeAttributes.NotPublic);
    }
}