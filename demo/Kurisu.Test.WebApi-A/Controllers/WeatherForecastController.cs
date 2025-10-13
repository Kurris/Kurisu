using Kurisu.AspNetCore;
using Kurisu.AspNetCore.Abstractions.UnifyResultAndValidation;
using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.MVC;
using Kurisu.AspNetCore.UnifyResultAndValidation;
using Kurisu.Test.Framework.DI.Dependencies.Abstractions;
using Kurisu.Test.Framework.DI.Dtos;
using Kurisu.Test.WebApi_A.Aops;
using Kurisu.Test.WebApi_A.AutoReload;
using Kurisu.Test.WebApi_A.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Kurisu.Test.WebApi_A.Controllers;

/// <summary>
/// 这是天气
/// </summary>
[ApiDefinition("test")]
[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ITestService _testService;
    private readonly IGenericsGet<Dog> _dogService;
    private readonly IEventBus _eventBus;
    private readonly IFrameworkExceptionHandlers _exceptionHandlers;

    public WeatherForecastController(IConfiguration configuration, ITestService testService,
        IGenericsGet<Dog> dogService,
        IEventBus eventBus,
        IFrameworkExceptionHandlers exceptionHandlers)
    {
        _configuration = configuration;
        _testService = testService;
        _dogService = dogService;
        _eventBus = eventBus;
        _exceptionHandlers = exceptionHandlers;
    }

    [TestAop]
    [Log("测试", DisableResponseLogout = true)]
    [AllowAnonymous]
    [HttpPost("config123")]
    //[IgnoreTenant]
    [return: TestReturnParameterAOP]
    public virtual async Task<IApiResult> GetConfig([CheckMyLessonAOP] Lesson lesson)
    {
        _dogService.Say();
        var s = DateTime.Now.ToString("D");
        var dict = _exceptionHandlers.GetMethods();
        await _testService.SayAsync();
        var options = _configuration.GetSection("ReloadConfigOptions").Get<SignalROptions>();
        return DefaultApiResult.Success(options);
    }
}

public interface ILessonId
{
    public long LessonId { get; set; }
}

public class Lesson : ILessonId
{
    public long LessonId { get; set; }
}

public class TestResult
{
    [JsonProperty("URL")]
    public string Url { get; set; }

    public string UserName { get; set; }
    public string Password { get; set; }

    public string Product { get; set; }

    public string Service { get; set; }

    public string Env { get; set; }
}