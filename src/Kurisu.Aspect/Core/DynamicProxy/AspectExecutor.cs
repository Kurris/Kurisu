using Kurisu.Aspect.Core.Utils;
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

    public TResult Invoke<TResult>(AspectExecutorContext executorContext)
    {
        var context = executorContext.ToAspectRuntimeContext(_serviceProvider);

        var aspectBuilder = _aspectBuilderFactory.Create(context.ServiceMethod, context.ImplementationMethod);
        var task = aspectBuilder.Build()(context);

        if (!task.IsCompleted)
        {
            // try to avoid potential deadlocks.
            Task.Run(() => task).GetAwaiter().GetResult();
        }
        
        if (task.IsFaulted)
        {
            task.RethrowIfFaulted();
        }

        return (TResult)context.ReturnValue;
    }

    public async Task<TResult> InvokeTask<TResult>(AspectExecutorContext executorContext)
    {
        var context = executorContext.ToAspectRuntimeContext(_serviceProvider);

        var aspectBuilder = _aspectBuilderFactory.Create(context.ServiceMethod, context.ImplementationMethod);
        var task = aspectBuilder.Build()(context);

        if (!task.IsCompleted)
        {
            await task;
        }
        
        if (task.IsFaulted)
        {
            task.RethrowIfFaulted();
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

    public async ValueTask<TResult> InvokeValueTask<TResult>(AspectExecutorContext executorContext)
    {
        var context = executorContext.ToAspectRuntimeContext(_serviceProvider);

        var aspectBuilder = _aspectBuilderFactory.Create(context.ServiceMethod, context.ImplementationMethod);
        var task = aspectBuilder.Build()(context);
        
        if (!task.IsCompleted)
        {
            await task;
        }
        
        if (task.IsFaulted)
        {
            task.RethrowIfFaulted();
        }

        return context.ReturnValue switch
        {
            null => default,
            ValueTask<TResult> taskWithResult => taskWithResult.Result,
            ValueTask => default,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}