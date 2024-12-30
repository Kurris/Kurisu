using Kurisu.Aspect.DynamicProxy;

namespace Kurisu.Aspect.Core.DynamicProxy;

public sealed class AspectBuilder
{
    private readonly IList<Func<AspectDelegate, AspectDelegate>> _delegates;
    private readonly AspectDelegate _complete;
    private AspectDelegate _aspectDelegate;

    public AspectBuilder(AspectDelegate complete)
    {
        _complete = complete ?? throw new ArgumentNullException(nameof(complete));
        _delegates = new List<Func<AspectDelegate, AspectDelegate>>();
    }

    public void AddAspectDelegate(Func<AspectContext, AspectDelegate, Task> interceptorInvoke)
    {
        if (interceptorInvoke == null) throw new ArgumentNullException(nameof(interceptorInvoke));
        _delegates.Add(next => context => interceptorInvoke(context, next));
    }

    public AspectDelegate Build()
    {
        if (_aspectDelegate != null) return _aspectDelegate;

        var @delegate = _complete;
        var count = _delegates.Count;
        for (var i = count - 1; i > -1; i--)
        {
            @delegate = _delegates[i](@delegate);
        }

        _aspectDelegate = @delegate;
        return _aspectDelegate;
    }
}