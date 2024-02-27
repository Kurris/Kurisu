using System.ComponentModel.DataAnnotations;
using Dlhis.Entity.Basic.Entity;
using Kurisu.AspNetCore.MVC;
using Kurisu.AspNetCore.Utils;
using Kurisu.SqlSugar.Services;
using Kurisu.Test.WebApi_A.Entity;
using Kurisu.Test.WebApi_A.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Kurisu.Test.WebApi_A.Controllers;

/// <summary>
/// 这是天气
/// </summary>
[ApiDefinition("天气api")]
[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IDbContext _dbContext;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 测试
    /// </summary>
    [HttpPost("test")]
    [Log("测试", Diff = true)]
    public async Task<object> Test(TestRequest request)
    {
        var types = await _dbContext.Queryable<BasicTypeEntity>().ToListAsync();
        var faceInfo = await _dbContext.ChangeDb("face").Queryable<GainfoFaceEntity>().FirstAsync();
        types = await _dbContext.Queryable<BasicTypeEntity>().ToListAsync();

        return types;
    }
}