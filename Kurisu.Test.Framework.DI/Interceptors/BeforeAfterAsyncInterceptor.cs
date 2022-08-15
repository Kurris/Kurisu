using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Kurisu.Test.Framework.DI.Interceptors
{
    public class BeforeAfterAsyncInterceptor : AsyncInterceptorBase
    {
        protected override async Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task> proceed)
        {
            Console.WriteLine("before");
            // Cannot simply return the the task, as any exceptions would not be caught below.
            await proceed(invocation, proceedInfo).ConfigureAwait(false);
            Console.WriteLine("after");
        }

        protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
        {
            Console.WriteLine("before");
            var result = await proceed(invocation, proceedInfo).ConfigureAwait(false);
            Console.WriteLine("after");
            return result;
        }
    }
}