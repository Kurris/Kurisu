using System;
using System.Threading.Tasks;
using Kurisu.Authorization;
using Kurisu.Cors;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.UnitOfWork.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TestApi.Entities;
using TestApi.Services;

namespace TestApi.Controllers
{
    [ApiController]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ITestService _service;
        private readonly IOptions<CorsAppSetting> _options;
        private readonly IOptions<JwtAppSetting> _jwt;
        private readonly IMasterDbImplementation _db;
        private readonly AService _aService;

        public WeatherForecastController(ITestService testService
            , IOptions<CorsAppSetting> options
            , IOptions<JwtAppSetting> jwt
            , IMasterDbImplementation db
            , AService aService)
        {
            _service = testService;
            _options = options;
            _jwt = jwt;
            _db = db;
            _aService = aService;
        }

        [UnitOfWork]
        [HttpGet("api/config")]
        public async Task<string> GetTestConfigurationMonitor()
        {
            await _db.AddAsync(new User()
            {
                Age = 23,
                Id = new Guid(),
                Name = "ligy"
            });

            // var users = await _db.FindListAsync<User>();

            await _aService.Delete();

            return "3213123";
        }
    }
}