using System;
using Kurisu.MVC;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using WebApiDemo.Services.DI;

namespace WebApiDemo.Controllers
{
    [ApiDefinition("DI测试")]
    [ApiController]
    [Route("[controller]")]
    public class DependencyInjectionController : ControllerBase
    {
        private readonly Func<string, IScopeDependency, object> _funcNamedService;
        private readonly IServiceProvider _serviceProvider;
        private readonly SingletonService2 _singletonService2;

        public DependencyInjectionController(Func<string, IScopeDependency, object> funcNamedService
            , IServiceProvider serviceProvider
            , SingletonService2 singletonService2)
        {
            _funcNamedService = funcNamedService;
            _serviceProvider = serviceProvider;
            _singletonService2 = singletonService2;
        }

        /// <summary>
        /// 命名注册
        /// </summary>
        /// <returns></returns>
        [HttpGet("namedRegister")]
        public string NamedRegister()
        {
            var ligyNamedService = _funcNamedService.Get<INamedService>("ligy");
            var ligy = ligyNamedService.Hello();

            var xiaoNamedService = _funcNamedService.Get<INamedService>("xiao");
            var xiao = xiaoNamedService.Hello();

            return ligy + " and " + xiao;
        }

        /// <summary>
        /// 单例注入
        /// </summary>
        /// <returns></returns>
        [HttpGet("singletonRegister")]
        public string SingletonRegister()
        {
            _serviceProvider.GetService<ISingletonService>();
            _serviceProvider.GetService<ISingletonService>();
            _singletonService2.Hello();
            return _serviceProvider.GetService<ISingletonService>().Hello();
        }
    }
}