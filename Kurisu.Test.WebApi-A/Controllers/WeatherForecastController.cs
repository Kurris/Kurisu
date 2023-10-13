using Kurisu.DataAccess.Functions.Default.Abstractions;
using Kurisu.DataAccess.Functions.Default;
using Kurisu.MVC;
using Kurisu.Test.WebApi_A.Dtos;
using Kurisu.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.Test.WebApi_A.Controllers;

[ApiDefinition("天气api")]
[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly ITryTestService _testService;
    private readonly DefaultAppDbContext<IDbWrite> _context;
    private readonly IUserApi _userApi;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, ITryTestService testService,
        DefaultAppDbContext<IDbWrite> context, IUserApi userApi)
    {
        _logger = logger;
        _testService = testService;
        _context = context;
        _userApi = userApi;
    }

    [HttpGet("test")]
    public async Task<object> TestInterceptor()
    {
        return await _userApi.GetHtmlAsync("", 1, 10);
    }

    /// <summary>
    /// 添加
    /// </summary>
    [HttpPost]
    public async Task Add()
    {
        await _context.AddAsync(new Entity.Test
        {
            Id = SnowFlakeHelper.Instance.NextId(),
            Name = "222"
        });

        await _context.SaveChangesAsync();
    }


    [HttpGet]
    public async Task<List<Entity.Test>> List()
    {
        return await _context.Set<Entity.Test>().ToListAsync();
    }


    [HttpGet("page")]
    public async Task<List<Entity.Test>> GetPages([FromQuery] NameInput input)
    {
        var count = await _context.Set<Entity.Test>().CountAsync();
        return await _context.Set<Entity.Test>().Skip((input.PageIndex - 1) * input.PageSize).Take(input.PageSize).ToListAsync();
    }
}