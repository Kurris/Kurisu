using System;
using Kurisu.Test.Framework.DependencyInjection.Dependencies.Abstractions;
using Kurisu.Test.Framework.DependencyInjection.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

[Trait("di", "dependencies")]
public class TestDependencies
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IGenericsGet<Cat> _catGenericsGet;
    private readonly IGenericsGet<Dog> _dogGenericsGet;

    public TestDependencies(IServiceProvider serviceProvider
        , IGenericsGet<Cat> catGenericsGet
        , IGenericsGet<Dog> dogGenericsGet)
    {
        _serviceProvider = serviceProvider;
        _catGenericsGet = catGenericsGet;
        _dogGenericsGet = dogGenericsGet;
    }

    [Fact]
    public void Test_Singleton_Return_Same()
    {
        var str1 = _serviceProvider.GetService<ITestSingleton>().Get();
        var str2 = _serviceProvider.GetService<ITestSingleton>().Get();
        var str3 = _serviceProvider.GetService<ITestSingleton>().Get();

        Assert.Equal(str1, str2);
        Assert.Equal(str2, str3);
    }


    [Fact]
    public void T()
    {
        var arr = new[] {0, 1, 2, 3, 4};

        var userName = "ligy";
        var code = Math.Abs(userName.GetHashCode() % 5);
        Assert.Contains(code, arr);
    }


    [Fact]
    public void Test_ScopeWithInterceptor()
    {
       // _scopeWithInterceptor.Get();
    }


    [Fact]
    public void Test_GenericsRegister()
    {
        var catSay = _catGenericsGet.Say();
        Assert.Equal("mewu", catSay);

        var dogSay = _dogGenericsGet.Say();
        Assert.Equal("wan", dogSay);
    }

}