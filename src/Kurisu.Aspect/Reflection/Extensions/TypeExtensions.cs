using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kurisu.Aspect.Reflection.Extensions;

internal static class TypeExtensions
{
    private static readonly ConcurrentDictionary<TypeInfo, bool> _isTaskOfTCache = new();
    private static readonly ConcurrentDictionary<TypeInfo, bool> _isValueTaskOfTCache = new();
    private static readonly Type _voidTaskResultType = Type.GetType("System.Threading.Tasks.VoidTaskResult", false);

    public static MethodInfo GetMethodBySignature(this TypeInfo typeInfo, MethodSignature signature)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return typeInfo.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).FirstOrDefault(m => new MethodSignature(m) == signature);
    }

    public static MethodInfo GetDeclaredMethodBySignature(this TypeInfo typeInfo, MethodSignature signature)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return typeInfo.DeclaredMethods.FirstOrDefault(m => new MethodSignature(m) == signature);
    }

    public static object GetDefaultValue(this TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        if (typeInfo.AsType() == typeof(void))
        {
            return null;
        }

        switch (Type.GetTypeCode(typeInfo.AsType()))
        {
            case TypeCode.Object:
            case TypeCode.DateTime:
                return typeInfo.IsValueType ? Activator.CreateInstance(typeInfo.AsType()) : null;

            case TypeCode.Empty:
            case TypeCode.String:
                return null;

            case TypeCode.Boolean:
            case TypeCode.Char:
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
                return 0;

            case TypeCode.Int64:
            case TypeCode.UInt64:
                return 0;

            case TypeCode.Single:
                return default(Single);

            case TypeCode.Double:
                return default(Double);

            case TypeCode.Decimal:
                return new Decimal(0);

            default:
                throw new InvalidOperationException("Code supposed to be unreachable.");
        }
    }

    public static object GetDefaultValue(this Type type)
    {
        return type?.GetTypeInfo().GetDefaultValue();
    }

    public static bool IsVisible(this TypeInfo typeInfo)
    {
        if (typeInfo == null) throw new ArgumentNullException(nameof(typeInfo));

        if (typeInfo.IsNested)
        {
            if (!typeInfo.DeclaringType!.GetTypeInfo().IsVisible())
            {
                return false;
            }

            if (!typeInfo.IsVisible || !typeInfo.IsNestedPublic)
            {
                return false;
            }
        }
        else
        {
            if (!typeInfo.IsVisible || !typeInfo.IsPublic)
            {
                return false;
            }
        }

        // ReSharper disable once InvertIf
        if (typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition)
        {
            foreach (var argument in typeInfo.GenericTypeArguments)
            {
                if (!argument.GetTypeInfo().IsVisible())
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static bool IsTask(this TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return typeInfo.AsType() == typeof(Task);
    }

    public static bool IsTaskWithResult(this TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return _isTaskOfTCache.GetOrAdd(typeInfo, info => info.IsGenericType && typeof(Task).GetTypeInfo().IsAssignableFrom(info));
    }

    public static bool IsTaskWithVoidTaskResult(this TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return typeInfo.GenericTypeArguments.Length > 0 && typeInfo.GenericTypeArguments[0] == _voidTaskResultType;
    }

    public static bool IsValueTask(this TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return typeInfo.AsType() == typeof(ValueTask);
    }

    public static bool IsValueTaskWithResult(this TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return _isValueTaskOfTCache.GetOrAdd(typeInfo, info => info.IsGenericType && info.GetGenericTypeDefinition() == typeof(ValueTask<>));
    }

    public static bool IsNullableType(this Type type)
    {
        return type.GetTypeInfo().IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public static bool IsTupleType(this Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.IsGenericType && typeof(ITuple).IsAssignableFrom(type.GetTypeInfo().GetGenericTypeDefinition());
    }
}