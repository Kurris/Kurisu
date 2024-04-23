using Dlhis.External.MealSupplement.Channel;
using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.Utils;
using Kurisu.SqlSugar.Services;
using Kurisu.Test.WebApi_A.AutoReload;
using Kurisu.Test.WebApi_A.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Kurisu.Test.WebApi_A.Controllers;

/// <summary>
/// 这是天气
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IDbContext _dbContext;
    private readonly RedisCache _redisCache;
    private readonly IEventBus _eventBus;
    private readonly IConfiguration _configuration;
    private readonly IJipApi _jipApi;

    public WeatherForecastController(ILogger<WeatherForecastController> logger,
        IDbContext dbContext,
        RedisCache redisCache,
        IEventBus eventBus,
        IConfiguration configuration,
        IJipApi jipApi)
    {
        _dbContext = dbContext;
        _redisCache = redisCache;
        _eventBus = eventBus;
        _configuration = configuration;
        _jipApi = jipApi;
    }

    /// <summary>
    /// 测试
    /// </summary>
    //[Authorize]
    [IgnoreDataCase]
    [HttpPost("test")]
    //[Log("测试", Diff = true)]
    //[DataPermission(true, "a")]
    public async Task<IDictionary<string, string>> Test(TestRequest request)
    {
        return new Dictionary<string, string>
        {
            ["WB-0741"] = "ligy"
        };
        var face = await _dbContext.Queryable<BasicTypeEntity>().Select(x => new FaceResponse
        {
            Face = "441900199703026534"
        }).FirstAsync();

        //var response = await _jipApi.CallbackAsync(new List<TestRequest>
        //{
        //    new()
        //    {
        //        uniqueId = "123",
        //        msg = "test",
        //        status = "SUCCESS"
        //    }
        //});

        //retryInterval: TimeSpan.FromMilliseconds(500)
        //var deviceNo = "";
        //using (var handler = _redisCache.Lock("LockDevice_" + deviceNo))
        //{
        //    if (handler.Acquired)
        //    {
        //        await Task.Delay(TimeSpan.FromSeconds(20));
        //    }
        //    else
        //    {
        //        //记录没有处理的任务
        //    }
        //}

        await _eventBus.PublishAsync(new MealSupplementMessage { });

        return null;
    }


    /// <summary>
    /// 测试
    /// </summary>
    [Authorize]
    [HttpPost("test1")]
    public async Task<object> TestGet(string request)
    {
        var types = await _dbContext.Queryable<CanteenSettingEntity>()
            .LeftJoin<BasicTypeEntity>((a, b) => a.Catalog == b.Type)
            .Select((a, b) => new
            {
                a.Catalog,
                b.Type
            })
            .ToListAsync();
        return types;
    }

    [AllowAnonymous]
    [HttpGet("config")]
    public async Task<object> GetConfig()
    {
        var options = _configuration.GetSection("ReloadConfigOptions").Get<SignalROptions>();
        return await Task.FromResult(options);
    }
}