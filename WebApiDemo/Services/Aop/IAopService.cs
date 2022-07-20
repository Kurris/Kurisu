using Microsoft.Extensions.DependencyInjection;

namespace WebApiDemo.Services.Aop
{
    public interface IAopService : ITransientDependency
    {
        string Hello();
    }
}