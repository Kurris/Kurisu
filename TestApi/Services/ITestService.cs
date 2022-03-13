using System.Threading.Tasks;
using Kurisu.DependencyInjection.Abstractions;
using Kurisu.DependencyInjection.Attributes;

namespace TestApi.Services
{
    public interface ITestService : ITransientDependency
    {
        string Get();

        void Set();
    }
}