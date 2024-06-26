﻿using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Kurisu.Aspect.DynamicProxy;
using Kurisu.Aspect.Reflection.Extensions;
using Kurisu.Aspect.Reflection.Reflectors;

namespace Kurisu.Aspect.Core.DynamicProxy.Extensions;

public static class AspectRuntimeContextExtensions
{
    private static readonly ConcurrentDictionary<MethodInfo, bool> _isAsyncCache = new();
    internal static ConcurrentDictionary<MethodInfo, MethodReflector> ReflectorTable { get; } = new();

    private static readonly ConcurrentDictionary<TypeInfo, Func<object, object>> _resultFuncCache = new();

    private static readonly ConcurrentDictionary<TypeInfo, Func<object, Task>> _asTaskFuncCache = new();

    public static ValueTask AwaitIfAsync(this AspectContext aspectContext)
    {
        return AwaitIfAsync(aspectContext.ReturnValue);
    }

    private static async ValueTask AwaitIfAsync(object returnValue)
    {
        switch (returnValue)
        {
            case null:
                break;
            case Task task:
                await task;
                break;
            case ValueTask valueTask:
                await valueTask;
                break;
            default:
            {
                var type = returnValue.GetType().GetTypeInfo();
                if (type.IsValueTaskWithResult())
                {
                    await ValueTaskWithResultToTask(returnValue, type);
                }

                break;
            }
        }
    }

    public static bool IsAsync(this AspectContext aspectContext)
    {
        if (aspectContext == null)
        {
            throw new ArgumentNullException(nameof(aspectContext));
        }

        var isAsyncFromMetaData = _isAsyncCache.GetOrAdd(aspectContext.ServiceMethod, IsAsyncFromMetaData);
        if (isAsyncFromMetaData)
        {
            return true;
        }

        return aspectContext.ReturnValue != null && IsAsyncType(aspectContext.ReturnValue.GetType().GetTypeInfo());
    }

    public static async Task<T> UnwrapAsyncReturnValue<T>(this AspectContext aspectContext)
    {
        return (T)await UnwrapAsyncReturnValue(aspectContext);
    }

    public static Task<object> UnwrapAsyncReturnValue(this AspectContext aspectContext)
    {
        if (aspectContext == null)
        {
            throw new ArgumentNullException(nameof(aspectContext));
        }

        if (!aspectContext.IsAsync())
        {
            throw new AspectInvocationException(aspectContext, "This operation only support asynchronous method.");
        }

        var returnValue = aspectContext.ReturnValue;
        if (returnValue == null)
        {
            return null;
        }

        var returnTypeInfo = returnValue.GetType().GetTypeInfo();
        return Unwrap(returnValue, returnTypeInfo);
    }

    // value should be ValueTask<T>
    private static Task ValueTaskWithResultToTask(object value, TypeInfo valueTypeInfo)
    {
        // NOTE: if we use "await (dynamic)value" to await a ValueTask<T> when T is non-public, we will get an RuntimeBinderException that says
        // 'System.ValueType' does not contain a definition for 'GetAwaiter'.
        // So we have to convert ValueTask<T> to Task and then await it.
        // Please fix this logic if there is a better solution.
        var func = _asTaskFuncCache.GetOrAdd(valueTypeInfo, k =>
        {
            var parameter = Expression.Parameter(typeof(object), "type");
            var convertedParameter = Expression.Convert(parameter, k);
            var method = k.GetMethod(nameof(ValueTask<int>.AsTask));
            var property = Expression.Call(convertedParameter, method!);
            var convertedProperty = Expression.Convert(property, typeof(Task));
            var exp = Expression.Lambda<Func<object, Task>>(convertedProperty, parameter);
            return exp.Compile();
        });
        return func(value);
    }

    // mark this method as "internal" for testing.
    private static Func<object, object> CreateFuncToGetTaskResult(Type typeInfo)
    {
        var parameter = Expression.Parameter(typeof(object), "type");
        var convertedParameter = Expression.Convert(parameter, typeInfo);
        var property = Expression.Property(convertedParameter, nameof(Task<int>.Result));
        var convertedProperty = Expression.Convert(property, typeof(object));
        var exp = Expression.Lambda<Func<object, object>>(convertedProperty, parameter);
        return exp.Compile();
    }

    // value should be Task<T> or ValueTask<T>
    private static object GetTaskResult(object value, TypeInfo valueTypeInfo)
    {
        var func = _resultFuncCache.GetOrAdd(valueTypeInfo, CreateFuncToGetTaskResult);
        return func(value);
    }

    private static async Task<object> Unwrap(object value, TypeInfo valueTypeInfo)
    {
        object result;

        if (valueTypeInfo.IsTaskWithVoidTaskResult())
        {
            return null;
        }
        else if (valueTypeInfo.IsTaskWithResult())
        {
            // NOTE: we can not use "result = (object)(await (dynamic)value)" here,
            // because when T of Task<T> is non-public, we will get a RuntimeBinderException that says "Cannot implicitly convert type 'void' to 'object'".
            await (Task)value;
            result = GetTaskResult(value, valueTypeInfo);
        }
        else if (valueTypeInfo.IsValueTaskWithResult())
        {
            // NOTE: we can not use "result = (object)(await (dynamic)value)" here,
            // because when T of ValueTask<T> is non-public, we will get a RuntimeBinderException that says "'System.ValueType' does not contain a definition for 'GetAwaiter'".
            await ValueTaskWithResultToTask(value, valueTypeInfo);
            result = GetTaskResult(value, valueTypeInfo);
        }
        else if (value is Task)
        {
            return null;
        }
        else if (value is ValueTask)
        {
            return null;
        }
        else
        {
            result = value;
        }

        if (result == null)
        {
            return null;
        }

        var resultTypeInfo = result.GetType().GetTypeInfo();
        if (IsAsyncType(resultTypeInfo))
        {
            return await Unwrap(result, resultTypeInfo);
        }

        return result;
    }

    private static bool IsAsyncFromMetaData(MethodInfo method)
    {
        return IsAsyncType(method.ReturnType.GetTypeInfo());
    }

    private static bool IsAsyncType(TypeInfo typeInfo)
    {
        //return typeInfo.IsTask() || typeInfo.IsTaskWithResult() || typeInfo.IsValueTask();
        if (typeInfo.IsTask())
        {
            return true;
        }

        if (typeInfo.IsTaskWithResult())
        {
            return true;
        }

        if (typeInfo.IsValueTask())
        {
            return true;
        }

        if (typeInfo.IsValueTaskWithResult())
        {
            return true;
        }

        return false;
    }
}