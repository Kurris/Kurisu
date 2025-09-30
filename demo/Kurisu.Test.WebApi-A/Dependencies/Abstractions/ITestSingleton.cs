using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DI.Dependencies.Abstractions;

public interface ITestSingleton
{
    string Get();
}