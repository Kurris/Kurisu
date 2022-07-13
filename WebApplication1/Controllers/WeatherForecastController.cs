using System.Collections.Generic;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.UnitOfWork.Attributes;
using Kurisu.Elasticsearch.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;

        // private readonly IElasticSearchService _esService;
        private readonly ISlaveDb _slaveDb;

        public WeatherForecastController(ILogger<WeatherForecastController> logger
            // , IElasticSearchService esService
            , ISlaveDb slaveDbService)
        {
            _logger = logger;
            // _esService = esService;
            _slaveDb = slaveDbService;
        }


        // [UnitOfWork]
        [HttpGet("menus")]
        public async Task<IEnumerable<Menu>> GetMenus()
        {
           return await _slaveDb.Queryable<Menu>().ToListAsync();
           // return null;
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