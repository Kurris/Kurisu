using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.UnitOfWork.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.Test.WebApi_A.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IDbService _dbService;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IDbService dbService)
    {
        _logger = logger;
        _dbService = dbService;
    }

    //[UnitOfWork(IsAutomaticSaveChanges = false)]
    [Authorize]
    [HttpGet]
    public async Task<List<Entity.Test>> GetWeatherForecast()
    {
        var t = new Entity.Test()
        {
            Name = "test",
        };

        await _dbService.AddAsync(t);
        //await _dbService.SaveChangesAsync();

        return await _dbService.AsNoTracking<Entity.Test>().ToListAsync();
    }
}