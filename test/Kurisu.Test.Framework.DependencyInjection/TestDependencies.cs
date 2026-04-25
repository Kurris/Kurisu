using System;
using Kurisu.Test.Framework.DependencyInjection.Dependencies.Abstractions;
using Kurisu.Test.Framework.DependencyInjection.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

[Trait("di", "dependencies")]
public class TestDependencies
{
    [Fact]
    public void Test_Singleton_Return_Same()
    {
        var sp = TestHelper.GetServiceProvider();
        using (var scope = sp.CreateScope())
        {
            var str1 = scope.ServiceProvider.GetService<ITestSingleton>().Get();
            var str2 = scope.ServiceProvider.GetService<ITestSingleton>().Get();
            var str3 = scope.ServiceProvider.GetService<ITestSingleton>().Get();

            Assert.Equal(str1, str2);
            Assert.Equal(str2, str3);
        }
    }


    [Fact]
    public void Test_GenericsRegister()
    {
        var sp = TestHelper.GetServiceProvider();
        using (var scope = sp.CreateScope())
        {
            var catSay = scope.ServiceProvider.GetService<IGenericsGet<Cat>>().Say();
            Assert.Equal("mewu", catSay);

            var dogSay = scope.ServiceProvider.GetService<IGenericsGet<Dog>>().Say();
            Assert.Equal("wan", dogSay);
        }
    }
}