using System;
using Kurisu.Test.Framework.DependencyInjection.Dependencies.Abstractions;

namespace Kurisu.Test.Framework.DependencyInjection.Dependencies;

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