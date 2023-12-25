using System.ComponentModel.DataAnnotations;
using Kurisu.AspNetCore.MVC;
using Kurisu.RemoteCall.Attributes;
using Kurisu.Test.WebApi_A.Dtos;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;

namespace Kurisu.Test.WebApi_A.Controllers;

/// <summary>
/// 这是天气
/// </summary>
[ApiDefinition("天气api")]
[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly ITryTestService _testService;
    private readonly ITestApi _userApi;
    private readonly ISqlSugarClient _sugarClient;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, ITryTestService testService, ITestApi userApi, ISqlSugarClient sugarClient)
    {
        _logger = logger;
        _testService = testService;
        _userApi = userApi;
        _sugarClient = sugarClient;
    }

    /// <summary>
    /// 测试
    /// </summary>
    // [Authorize]
    [HttpGet("test")]
    public async Task<object> TestInterceptor()
    {
        //await _testService.Say();
        return await _userApi.GetString();
        //await _userApi.SendMsg(new NameInput() { Name = "xiaoshang" });
        ////await _userApi.GetH();
        ////await _userApi.PutAsync(2);
        ////return await _userApi.GetHtmlAsync(new NameInput() { Name = "ligy" }, 1, 10);
        //var filePath = @"E:\dl\晶科\钉钉.txt";
        //await _userApi.UploadAsync(await System.IO.File.ReadAllBytesAsync(filePath), "file", Path.GetFileName(filePath));
    }

    [HttpGet("test-something")]
    public TestDto GetTest()
    {
        return new TestDto()
        {
            LogDate = DateTime.Now,
        };
    }

    [HttpPost("upload")]
    public async Task UploadFile(IFormFile file)
    {
    }


    [HttpPost("post")]
    public async Task UploadFile(string nameInput)
    {
    }

    [HttpPut("{id}")]
    public async Task UploadFile(int id)
    {
    }

    [HttpGet("string")]
    public async Task<string> GetString([Required] string whatStr)
    {
        return await Task.FromResult("测试string");
    }
}