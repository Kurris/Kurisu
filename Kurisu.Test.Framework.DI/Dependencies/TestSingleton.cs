using System;
using Kurisu.Test.Framework.DI.Dependencies.Abstractions;

namespace Kurisu.Test.Framework.DI.Dependencies
{
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
}