using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Kurisu.DependencyInjection.Attributes;
using Kurisu.Proxy;
using Kurisu.Proxy.Attributes;
using Microsoft.Extensions.Logging;
using DispatchProxy = Kurisu.Proxy.DispatchProxy;

namespace TestApi.Services
{
    public class TestProxy : DispatchProxy
    {
        [Register]
        public ILogger<TestProxy>  Logger { get; set; }
        public override object Invoke(MethodInfo method, object[] args)
        {
            Logger?.LogInformation("sss");
            Console.WriteLine("方法被调用了");

            var result = base.Invoke(method, args);

            return result;
        }
    }
}