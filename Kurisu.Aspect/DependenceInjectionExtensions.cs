using Kurisu.Aspect.Core.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Aspect
{
    public static class DependenceInjectionExtensions
    {
        public static void AddAspect(this IServiceCollection services)
        {
            services.AddSingleton<IAspectExecutorFactory, AspectExecutorFactory>();
            services.AddSingleton<AspectBuilderFactory>();
            services.AddSingleton(typeof(AspectCaching<,>));
            services.AddSingleton<InterceptorCollector>();
        }
    }
}