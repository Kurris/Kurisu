using System.Reflection;
using Kurisu.Aspect.Core.DynamicProxy;

namespace Kurisu.Aspect.DynamicProxy
{
    public struct AspectExecutorContext
    {
        public MethodInfo ServiceMethod { get; }

        public MethodInfo TargetMethod { get; }

        public MethodInfo ProxyMethod { get; }

        public object TargetInstance { get; }

        public object ProxyInstance { get; }

        public object[] Parameters { get; }

        public AspectExecutorContext(MethodInfo serviceMethod, MethodInfo targetMethod, MethodInfo proxyMethod,
            object targetInstance, object proxyInstance,  object[] parameters)
        {
            ServiceMethod = serviceMethod;
            TargetMethod = targetMethod;
            ProxyMethod = proxyMethod;
            TargetInstance = targetInstance;
            ProxyInstance = proxyInstance;
            Parameters = parameters;
        }

        internal AspectRuntimeContext ToRuntimeAspectContext(IServiceProvider serviceProvider)
        {
            return new AspectRuntimeContext(serviceProvider, ServiceMethod, TargetMethod, ProxyMethod, TargetInstance, ProxyInstance, Parameters);
        }
    }
}