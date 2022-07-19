using System.Collections.Generic;
using System.Threading.Tasks;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.MVC;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Controllers
{
    [Authorize]
    [ApiController]
    [ApiDefinition("天气")]
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
        }
    }
}