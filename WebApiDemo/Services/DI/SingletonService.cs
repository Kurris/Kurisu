using System;
using Microsoft.Extensions.DependencyInjection;

namespace WebApiDemo.Services.DI
{
    public class SingletonService : ISingletonService
    {
        public SingletonService()
        {
            Console.WriteLine("hello singleton !");  
        }

        public string Hello()
        {
            return "hello singleton !";
        }
    }
}