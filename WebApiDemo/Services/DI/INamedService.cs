using Microsoft.Extensions.DependencyInjection;

namespace WebApiDemo.Services.DI
{
    public interface INamedService : IScopeDependency
    {
        string Hello();
    }
}