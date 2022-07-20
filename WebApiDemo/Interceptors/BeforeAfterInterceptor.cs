using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace WebApiDemo.Interceptors
{
    public class BeforeAfterInterceptor : AsyncInterceptorBase
    {
        protected override async Task InterceptAsync(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task> proceed)
        {
            try
            {
                Console.WriteLine("before");
                // Cannot simply return the the task, as any exceptions would not be caught below.
                await proceed(invocation, proceedInfo).ConfigureAwait(false);
                Console.WriteLine("after");
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        protected override async Task<TResult> InterceptAsync<TResult>(IInvocation invocation, IInvocationProceedInfo proceedInfo, Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
        {
            try
            {
                Console.WriteLine("before");
                // Cannot simply return the the task, as any exceptions would not be caught below.
                return await proceed(invocation, proceedInfo).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}