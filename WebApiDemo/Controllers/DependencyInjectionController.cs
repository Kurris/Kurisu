using System;
using System.Threading.Tasks;
using Kurisu;
using Kurisu.Channel.Abstractions;
using Kurisu.MVC;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using WebApiDemo.Channel.Messages;
using WebApiDemo.Services.DI;

namespace WebApiDemo.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [ApiDefinition("DI测试")]
    [ApiController]
    [Route("[controller]")]
    public class DependencyInjectionController : ControllerBase
    {
        private readonly INamedResolver _namedResolver;
        private readonly IServiceProvider _serviceProvider;
        private readonly SingletonService2 _singletonService2;
        private readonly IChannelPublisher _channel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="namedResolver"></param>
        /// <param name="serviceProvider"></param>
        /// <param name="singletonService2"></param>
        public DependencyInjectionController(INamedResolver namedResolver
            , IServiceProvider serviceProvider
            , SingletonService2 singletonService2
            , IChannelPublisher channel)
        {
            _namedResolver = namedResolver;
            _serviceProvider = serviceProvider;
            _singletonService2 = singletonService2;
            _channel = channel;
        }

        /// <summary>
        /// 命名注册
        /// </summary>
        /// <returns></returns>
        [HttpGet("namedRegister")]
        public string NamedRegister()
        {
            var ligyNamedService = _namedResolver.GetService<INamedService>("ligy");
            var ligy = ligyNamedService.Hello();

            var xiaoNamedService = _namedResolver.GetService<INamedService>("xiao");
            var xiao = xiaoNamedService.Hello();

            return ligy + " and " + xiao;
        }

        /// <summary>
        /// 单例注入
        /// </summary>
        /// <returns></returns>
        [HttpGet("singletonRegister")]
        public async Task<string> SingletonRegister()
        {
            _serviceProvider.GetService<ISingletonService>();
            _serviceProvider.GetService<ISingletonService>();
            App.ServiceProvider.GetService<ISingletonService>();
            await _channel.PublishAsync(new BobMessage
            {
                Name = "bob",
                Age = 25,
                Claims = new[]
                {
                    "杭州", "拱墅区"
                }
            });
            _singletonService2.Hello();
            return _serviceProvider.GetService<ISingletonService>().Hello();
        }
    }
}