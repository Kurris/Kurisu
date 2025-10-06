using System;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.Test.Framework.DependencyInjection.Dependencies.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DependencyInjection.Dependencies;

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