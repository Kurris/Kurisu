using System.Linq.Expressions;
using System.Reflection;

namespace Kurisu.Aspect.Reflection.Extensions;

internal static class InternalExtensions
{
    internal static MethodInfo GetMethodBySign(this TypeInfo typeInfo, MethodInfo method)
    {
        return typeInfo.DeclaredMethods.FirstOrDefault(m => m.ToString() == method.ToString());
    }

    internal static MethodInfo GetMethod<T>(Expression<T> expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        if (expression.Body is not MethodCallExpression methodCallExpression)
        {
            throw new InvalidCastException("Cannot be converted to MethodCallExpression");
        }

        return methodCallExpression.Method;
    }

    internal static MethodInfo GetMethod<T>(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return typeof(T).GetTypeInfo().GetMethod(name);
    }


    internal static bool IsReturnTask(this MethodInfo methodInfo)
    {
        return typeof(Task).GetTypeInfo().IsAssignableFrom(methodInfo.ReturnType.GetTypeInfo());
    }

    internal static Type UnWrapArrayType(this TypeInfo typeInfo)
    {
        if (typeInfo == null)
        {
            throw new ArgumentNullException(nameof(typeInfo));
        }

        return !typeInfo.IsArray 
            ? typeInfo.AsType() 
            : typeInfo.ImplementedInterfaces.First(x => x.GetTypeInfo().IsGenericType && x.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEnumerable<>)).GenericTypeArguments[0];
    }
}