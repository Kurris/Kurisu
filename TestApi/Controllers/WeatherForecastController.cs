using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kurisu;
using Kurisu.Authorization;
using Kurisu.Authorization.Attributes;
using Kurisu.Cors;
using Kurisu.Cors.Extensions;
using Kurisu.DependencyInjection.Abstractions;
using Kurisu.Proxy.Attributes;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TestApi.Services;

namespace TestApi.Controllers
{
    [AppAuthorize]
    [ApiController]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ITestService _service;
        private readonly IOptions<CorsAppSetting> _options;
        private readonly IOptions<JWTAppSetting> _jwt;

        public WeatherForecastController(
            ITestService testService
            , IOptions<CorsAppSetting> options
            , IOptions<JWTAppSetting> jwt
        )
        {
            _service = testService;
            _options = options;
            _jwt = jwt;
        }


        [HttpGet("api/config")]
        public string GetTestConfigurationMonitor()
        {
            _service.Set();
            return _service.Get();
        }
    }
}