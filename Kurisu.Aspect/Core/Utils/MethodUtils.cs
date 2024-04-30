using System.Linq.Expressions;
using System.Reflection;
using Kurisu.Aspect.Core.DynamicProxy;
using Kurisu.Aspect.DynamicProxy;
using Kurisu.Aspect.Reflection;
using Kurisu.Aspect.Reflection.Extensions;
using Kurisu.Aspect.Reflection.Reflectors;

namespace Kurisu.Aspect.Core.Utils;

internal static class MethodUtils
{
    internal static readonly MethodInfo CreateAspectExecutor = GetMethod<IAspectExecutorFactory>(nameof(IAspectExecutorFactory.Create));

    internal static readonly MethodInfo AspectActivatorInvoke = GetMethod<IAspectExecutor>(nameof(IAspectExecutor.Invoke));

    internal static readonly MethodInfo AspectActivatorInvokeTask = GetMethod<IAspectExecutor>(nameof(IAspectExecutor.InvokeTask));

    internal static readonly MethodInfo AspectActivatorInvokeValueTask = GetMethod<IAspectExecutor>(nameof(IAspectExecutor.InvokeValueTask));

    internal static readonly ConstructorInfo AspectExecutorContextCtor = typeof(AspectExecutorContext).GetTypeInfo().DeclaredConstructors.First();

    internal static readonly ConstructorInfo ObjectCtor = typeof(object).GetTypeInfo().DeclaredConstructors.Single();

    internal static readonly MethodInfo GetParameters = typeof(AspectExecutorContext).GetTypeInfo().GetMethod("get_Parameters");

    internal static readonly MethodInfo GetMethodReflector = GetMethod<Func<MethodInfo, MethodReflector>>(m => m.GetReflector());

    internal static readonly MethodInfo ReflectorInvoke = GetMethod<Func<MethodReflector, object, object[], object>>((r, i, a) => r.Invoke(i, a));

    private static MethodInfo GetMethod<T>(Expression<T> expression)
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

    private static MethodInfo GetMethod<T>(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return typeof(T).GetTypeInfo().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }
}