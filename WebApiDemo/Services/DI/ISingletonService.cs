using Microsoft.Extensions.DependencyInjection;

namespace WebApiDemo.Services.DI
{
    public interface ISingletonService : ISingletonDependency
    {
        string Hello();
    }
}