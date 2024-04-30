using Kurisu.Test.WebApi_A.AutoReload;
using Kurisu.Test.WebApi_A.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kurisu.Test.WebApi_A.Controllers;

/// <summary>
/// 这是天气
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ITestService _testService;

    public WeatherForecastController(
        IConfiguration configuration,
        ITestService testService)
    {
        _configuration = configuration;
        _testService = testService;
    }

    /// <summary>
    /// 测试
    /// </summary>
    //[Authorize]
    [HttpPost("test")]
    //[Log("测试", Diff = true)]
    //[DataPermission(true, "a")]
    public async Task<object> Test(TestRequest request)
    {
        await _testService.DoAsync();
        return await _testService.SayAsync();
    }


    /// <summary>
    /// 测试
    /// </summary>
    [Authorize]
    [HttpPost("test1")]
    public async Task<object> TestGet(string request)
    {
        return null;
    }

    [AllowAnonymous]
    [HttpGet("config")]
    public async Task<object> GetConfig()
    {
        var options = _configuration.GetSection("ReloadConfigOptions").Get<SignalROptions>();
        return await Task.FromResult(options);
    }
}