using System.Collections.Concurrent;
using System.Reflection;
using Kurisu.RemoteCall.Proxy.Abstractions;

namespace Kurisu.RemoteCall.Proxy;

/// <summary>
/// Castle.Core.AsyncInterceptor
/// </summary>
internal class AsyncDeterminationInterceptor : IInterceptor
{
    private delegate void GenericAsyncHandler(IProxyInvocation invocation, IAsyncInterceptor asyncInterceptor);

    private static readonly MethodInfo HandleAsyncMethodInfo = typeof(AsyncDeterminationInterceptor).GetMethod(nameof(HandleAsyncWithResult), BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly ConcurrentDictionary<Type, GenericAsyncHandler> GenericAsyncHandlers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDeterminationInterceptor"/> class.
    /// </summary>
    /// <param name="asyncInterceptor">The underlying <see cref="AsyncInterceptor"/>.</param>
    public AsyncDeterminationInterceptor(IAsyncInterceptor asyncInterceptor)
    {
        AsyncInterceptor = asyncInterceptor;
    }

    private IAsyncInterceptor AsyncInterceptor { get; }

    public void Intercept(IProxyInvocation invocation)
    {
        var methodType = GetMethodType(invocation.Method.ReturnType);

        switch (methodType)
        {
            case 1:
                AsyncInterceptor.InterceptAsync(invocation);
                return;
            case 2:
                GenericAsyncHandlers.GetOrAdd(invocation.Method.ReturnType, returnType =>
                    {
                        var taskReturnType = returnType.GetGenericArguments()[0];
                        var method = HandleAsyncMethodInfo.MakeGenericMethod(taskReturnType);
                        return method.CreateDelegate<GenericAsyncHandler>();
                    })
                    .Invoke(invocation, AsyncInterceptor);
                return;
            default:
                AsyncInterceptor.Intercept(invocation);
                return;
        }

        static int GetMethodType(Type returnType)
        {
            //0 synchronous ; 1 asynchronous action ; 2 asynchronous function  

            // If there's no return type, or it's not a task, then assume it's a synchronous method.
            if (returnType == typeof(void) || !typeof(Task).IsAssignableFrom(returnType))
                return 0;

            // The return type is a task of some sort, so assume it's asynchronous
            return returnType.GetTypeInfo().IsGenericType ? 2 : 1;
        }
    }

    private static void HandleAsyncWithResult<TResult>(IProxyInvocation invocation, IAsyncInterceptor asyncInterceptor)
    {
        asyncInterceptor.InterceptAsync<TResult>(invocation);
    }
}