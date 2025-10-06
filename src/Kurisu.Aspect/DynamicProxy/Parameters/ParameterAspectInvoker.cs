using AspectCore.Utils;

namespace AspectCore.DynamicProxy.Parameters;

internal class ParameterAspectInvoker
{
    private readonly IList<Func<ParameterAspectDelegate, ParameterAspectDelegate>> _delegates = new List<Func<ParameterAspectDelegate, ParameterAspectDelegate>>();

    public void AddDelegate(Func<ParameterAspectContext, ParameterAspectDelegate, Task> parameterAspectDelegate)
    {
        _delegates.Add(next => ctx => parameterAspectDelegate(ctx, next));
    }

    private ParameterAspectDelegate Build()
    {
        ParameterAspectDelegate invoke = ctx => TaskUtils.CompletedTask;

        foreach (var next in _delegates.Reverse())
        {
            invoke = next(invoke);
        }

        return invoke;
    }

    public Task Invoke(ParameterAspectContext context)
    {
        return Build()(context);
    }

    public void Reset()
    {
        _delegates.Clear();
    }
}