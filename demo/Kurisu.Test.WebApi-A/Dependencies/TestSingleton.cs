using System;
using Kurisu.AspNetCore.DependencyInjection.Attributes;
using Kurisu.Test.Framework.DI.Dependencies.Abstractions;

namespace Kurisu.Test.Framework.DI.Dependencies;

[DiInject(Lifetime = ServiceLifetime.Singleton)]
public class TestSingleton : ITestSingleton
{
    public TestSingleton()
    {
        Guid = Guid.NewGuid();
    }

    public Guid Guid { get; }

    public string Get()
    {
        return Guid.ToString();
    }
}