using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Kurisu.Cors;
using Kurisu.DataAccessor.UnitOfWork.Attributes;
using Kurisu.Elasticsearch;
using Kurisu.Elasticsearch.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IOptions<CorsAppSetting> _options;
        private readonly IElasticSearchService _esService;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IOptions<CorsAppSetting> options
            , IElasticSearchService esService)
        {
            _logger = logger;
            _options = options;
            _esService = esService;
        }


        [UnitOfWork]
        [HttpGet("a")]
        public async Task<object> GetA()
        {

            return null;
        }

        [HttpGet("b")]
        public string GetB()
        {
            return "b";
        }
    }

    public class Users
    {
        public int No { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Address { get; set; }
        public string Job { get; set; }
    }
}