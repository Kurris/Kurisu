using System.Runtime.ExceptionServices;
using Kurisu.Aspect.DynamicProxy;

namespace Kurisu.Aspect.Core.DynamicProxy;

public interface IAspectExecutor
{
    TResult Invoke<TResult>(AspectExecutorContext activatorContext);
    Task<TResult> InvokeTask<TResult>(AspectExecutorContext activatorContext);
    ValueTask<TResult> InvokeValueTask<TResult>(AspectExecutorContext activatorContext);
}

internal class AspectExecutor : IAspectExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AspectBuilderFactory _aspectBuilderFactory;

    public AspectExecutor(IServiceProvider serviceProvider, AspectBuilderFactory aspectBuilderFactory)
    {
        _serviceProvider = serviceProvider;
        _aspectBuilderFactory = aspectBuilderFactory;
    }

    public TResult Invoke<TResult>(AspectExecutorContext activatorContext)
    {
        var context = activatorContext.ToRuntimeAspectContext(_serviceProvider);
        try
        {
            var aspectBuilder = _aspectBuilderFactory.Create(context);
            var task = aspectBuilder.Build()(context);
            if (task.IsFaulted)
            {
                ExceptionDispatchInfo.Capture(task.Exception!.InnerException!).Throw();
            }

            if (!task.IsCompleted)
            {
                // try to avoid potential deadlocks.
                //NoSyncContextScope.Run(task);
                // task.GetAwaiter().GetResult();
            }

            return (TResult)context.ReturnValue;
        }
        catch (Exception ex)
        {
            throw new AspectInvocationException(context, ex);
        }
    }

    public async Task<TResult> InvokeTask<TResult>(AspectExecutorContext activatorContext)
    {
        var context = activatorContext.ToRuntimeAspectContext(_serviceProvider);
        try
        {
            var aspectBuilder = _aspectBuilderFactory.Create(context);
            var invoke = aspectBuilder.Build()(context);

            if (invoke.IsFaulted)
            {
                ExceptionDispatchInfo.Capture(invoke.Exception!.InnerException!).Throw();
            }

            if (!invoke.IsCompleted)
            {
                await invoke;
            }

            switch (context.ReturnValue)
            {
                case null:
                    return default;
                case Task<TResult> taskWithResult:
                    return taskWithResult.Result;
                case Task _:
                    return default;
                default:
                    throw new AspectInvocationException(context, $"Unable to cast object of type '{context.ReturnValue.GetType()}' to type '{typeof(Task<TResult>)}'.");
            }
        }
        catch (Exception ex)
        {
            if (ex is AspectInvocationException _)
                throw;

            throw new AspectInvocationException(context, ex);
        }
    }

    public async ValueTask<TResult> InvokeValueTask<TResult>(AspectExecutorContext activatorContext)
    {
        var context = activatorContext.ToRuntimeAspectContext(_serviceProvider);
        try
        {
            var aspectBuilder = _aspectBuilderFactory.Create(context);
            var invoke = aspectBuilder.Build()(context);

            if (invoke.IsFaulted)
            {
                ExceptionDispatchInfo.Capture(invoke.Exception!.InnerException!).Throw();
            }

            if (!invoke.IsCompleted)
            {
                await invoke;
            }

            return context.ReturnValue switch
            {
                null => default,
                ValueTask<TResult> taskWithResult => taskWithResult.Result,
                ValueTask => default,
                _ => throw new AspectInvocationException(context, $"Unable to cast object of type '{context.ReturnValue.GetType()}' to type '{typeof(ValueTask<TResult>)}'.")
            };
        }
        catch (Exception ex)
        {
            if (ex is AspectInvocationException _)
                throw;

            throw new AspectInvocationException(context, ex);
        }
    }
}