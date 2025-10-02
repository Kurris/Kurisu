using System.Collections;
using AspectCore.DynamicProxy;

namespace AspectCore.DependencyInjection
{
    [NonAspect, NonCallback]
    public interface IManyEnumerable<out T> : IEnumerable<T>, IEnumerable
    {
    }
}