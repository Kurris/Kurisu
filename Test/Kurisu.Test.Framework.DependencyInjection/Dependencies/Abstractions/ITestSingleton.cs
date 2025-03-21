using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DependencyInjection.Dependencies.Abstractions;

public interface ITestSingleton : ISingletonDependency
{
    string Get();
}