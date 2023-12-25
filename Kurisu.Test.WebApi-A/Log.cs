using Kurisu.Core.Proxy;
using Kurisu.Core.Proxy.Abstractions;

namespace Kurisu.Test.WebApi_A
{
    public class Log : Aop
    {
        private readonly ILogger<Log> _logger;

        public Log(ILogger<Log> logger)
        {
            _logger = logger;
        }

        protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
        {
            _logger.LogInformation("before");
            await proceed(invocation).ConfigureAwait(false);
            _logger.LogInformation("next");
        }

        protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
        {
            _logger.LogInformation("before");
            var result = await proceed(invocation).ConfigureAwait(false);
            _logger.LogInformation("next");
            return result;
        }
    }
}
