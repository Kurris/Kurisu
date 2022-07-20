using System;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiDemo.Services.DI
{
    public class SingletonService2 : ISingletonDependency
    {
        private readonly IServiceProvider _serviceProvider;

        public SingletonService2(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public string Hello()
        {
            return _serviceProvider.GetService<ISingletonService>().Hello();
        }
    }
}