using System.Collections.Concurrent;
using System.Reflection;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Kurisu.RemoteCall.Proxy.Enums;

namespace Kurisu.RemoteCall.Proxy.Internal;

/// <summary>
/// castle.core.asyncinterceptor
/// </summary>
internal class AsyncDeterminationInterceptor : IInterceptor
{
    private static readonly MethodInfo HandleAsyncMethodInfo =
     typeof(AsyncDeterminationInterceptor)
             .GetMethod(nameof(HandleAsyncWithResult), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly ConcurrentDictionary<Type, GenericAsyncHandler> GenericAsyncHandlers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDeterminationInterceptor"/> class.
    /// </summary>
    /// <param name="asyncInterceptor">The underlying <see cref="AsyncInterceptor"/>.</param>
    public AsyncDeterminationInterceptor(IAsyncInterceptor asyncInterceptor)
    {
        AsyncInterceptor = asyncInterceptor;
    }

    private delegate void GenericAsyncHandler(IProxyInvocation invocation, IAsyncInterceptor asyncInterceptor);

    public IAsyncInterceptor AsyncInterceptor { get; }

    public void Intercept(IProxyInvocation invocation)
    {
        MethodType methodType = GetMethodType(invocation.Method.ReturnType);

        switch (methodType)
        {
            case MethodType.AsyncAction:
                AsyncInterceptor.InterceptAsynchronous(invocation);
                return;
            case MethodType.AsyncFunction:
                GetHandler(invocation.Method.ReturnType).Invoke(invocation, AsyncInterceptor);
                return;
            case MethodType.Synchronous:
            default:
                AsyncInterceptor.InterceptSynchronous(invocation);
                return;
        }
    }

    private static MethodType GetMethodType(Type returnType)
    {
        // If there's no return type, or it's not a task, then assume it's a synchronous method.
        if (returnType == typeof(void) || !typeof(Task).IsAssignableFrom(returnType))
            return MethodType.Synchronous;

        // The return type is a task of some sort, so assume it's asynchronous
        return returnType.GetTypeInfo().IsGenericType ? MethodType.AsyncFunction : MethodType.AsyncAction;
    }


    private static GenericAsyncHandler GetHandler(Type returnType)
    {
        GenericAsyncHandler handler = GenericAsyncHandlers.GetOrAdd(returnType, CreateHandler);
        return handler;
    }

    private static GenericAsyncHandler CreateHandler(Type returnType)
    {
        Type taskReturnType = returnType.GetGenericArguments()[0];
        MethodInfo method = HandleAsyncMethodInfo.MakeGenericMethod(taskReturnType);
        return (GenericAsyncHandler)method.CreateDelegate(typeof(GenericAsyncHandler));
    }

    private static void HandleAsyncWithResult<TResult>(IProxyInvocation invocation, IAsyncInterceptor asyncInterceptor)
    {
        asyncInterceptor.InterceptAsynchronous<TResult>(invocation);
    }
}