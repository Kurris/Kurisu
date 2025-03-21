using Kurisu.Test.Framework.DI.Dependencies.Abstractions;
using Kurisu.Test.Framework.DI.Dtos;
using Kurisu.Test.WebApi_A.AutoReload;
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
    private readonly IGenericsGet<Dog> _dogService;

    public WeatherForecastController(IConfiguration configuration, IGenericsGet<Dog> dogService)
    {
        _configuration = configuration;
        _dogService = dogService;
    }

    /// <summary>
    /// 测试
    /// </summary>
    [HttpPost("test")]
    [Authorize]
    //[Log("测试", Diff = true)]
    //[DataPermission(true, "a")]
    public async Task Test(List<FaceResponse> request)
    {
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