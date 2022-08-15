using System;
using Kurisu.Test.Framework.DI.Dependencies.Abstractions;
using Kurisu.Test.Framework.DI.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kurisu.Test.Framework.DI
{
    [Trait("di", "dependencies")]
    public class TestDependencies
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestScopeWithInterceptor _scopeWithInterceptor;
        private readonly IGenericsGet<Cat> _catGenericsGet;
        private readonly IGenericsGet<Dog> _dogGenericsGet;

        public TestDependencies(IServiceProvider serviceProvider
            , ITestScopeWithInterceptor scopeWithInterceptor
            , IGenericsGet<Cat> catGenericsGet
            , IGenericsGet<Dog> dogGenericsGet)
        {
            _serviceProvider = serviceProvider;
            _scopeWithInterceptor = scopeWithInterceptor;
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
        public void Test_ScopeWithInterceptor()
        {
            _scopeWithInterceptor.Get();
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
}