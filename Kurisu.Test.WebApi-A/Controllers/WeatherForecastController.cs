using Kurisu.MVC;
using Kurisu.Test.WebApi_A.Dtos;
using Kurisu.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.Test.WebApi_A.Controllers;

[ApiDefinition("天气api")]
[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly DefaultShardingDbContext _context;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, DefaultShardingDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// 添加
    /// </summary>
    [HttpPost]
    public async Task Add()
    {
        await _context.Tests.AddAsync(new Entity.Test
        {
            Id = SnowFlakeHelper.Instance.NextId(),
            Name = "222"
        });

        await _context.SaveChangesAsync();
    }


    [Authorize]
    [HttpGet]
    public async Task<List<Entity.Test>> List()
    {
        return await _context.Tests.ToListAsync();
    }


    [HttpGet("page")]
    public async Task<List<Entity.Test>> GetPages([FromQuery] NameInput input)
    {
        var count = await _context.Tests.CountAsync();
        return await _context.Tests.Skip((input.PageIndex - 1) * input.PageSize).Take(input.PageSize).ToListAsync();
    }
}