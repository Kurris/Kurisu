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
using System.Runtime.CompilerServices;

namespace Kurisu.Test.WebApi_A.Controllers;

/// <summary>
/// 这是天气
/// </summary>
[AllowAnonymous]
[ApiDefinition("test")]
[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ITestService _testService;
    private readonly IGenericsGet<Dog> _dogService;
    private readonly IEventBus _eventBus;
    private readonly IFrameworkExceptionHandlers _exceptionHandlers;

    public WeatherController(IConfiguration configuration, ITestService testService,
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

    // ------------------------- 新增示例接口 -------------------------

    // 简单的 GET 返回字符串
    [HttpGet("ping")]
    [AllowAnonymous]
    public ActionResult<string> Ping()
    {
        return "pong";
    }

    // GET 带查询参数，返回原始类型
    [HttpGet("echo")]
    public ActionResult<int> Echo([FromQuery] int value)
    {
        return Ok(value);
    }

    // GET 路径参数，返回 DTO
    [HttpGet("{id:int}/dto")]
    public ActionResult<TestResult> GetTestResult([FromRoute] int id)
    {
        return new TestResult
        {
            Url = Request?.Path,
            UserName = $"user_{id}",
            Product = "Demo",
            Service = "Weather",
            Env = "dev"
        };
    }

    // 返回集合
    [HttpGet("list")]
    public ActionResult<IEnumerable<TestResult>> GetList()
    {
        var list = Enumerable.Range(1, 3).Select(i => new TestResult
        {
            Url = $"/api/weather/{i}",
            UserName = $"user{i}",
            Product = "Demo",
            Service = "Weather",
            Env = "dev"
        }).ToList();

        return Ok(list);
    }

    // POST 以 body 接收 DTO 并返回统一 IApiResult
    [HttpPost("create")]
    public async Task<IApiResult> Create([FromBody] TestResult model)
    {
        // 模拟异步处理
        await Task.Delay(10);
        return DefaultApiResult.Success(model);
    }

    // PUT 更新，返回 NoContent
    [HttpPut("{id:int}")]
    public IActionResult Update([FromRoute] int id, [FromBody] TestResult model)
    {
        // 处理更新逻辑
        return NoContent();
    }

    // DELETE
    [HttpDelete("{id:int}")]
    public IActionResult Delete([FromRoute] int id)
    {
        return Ok(new { Deleted = id });
    }

    // 文件上传示例
    [HttpPost("upload")]
    public async Task<IApiResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return DefaultApiResult.Error("文件为空");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        return DefaultApiResult.Success(new { Size = ms.Length, FileName = file.FileName });
    }

    // 流式返回 IAsyncEnumerable
    [HttpGet("stream")]
    public async IAsyncEnumerable<int> StreamNumbers([FromQuery] int count = 10, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; i++)
        {
            yield return i;
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }
    }

    // 简单的验证示例
    [HttpPost("validate")]
    public IActionResult ValidateLesson([FromBody] Lesson lesson)
    {
        if (lesson == null || lesson.LessonId <= 0)
            return BadRequest("LessonId must be greater than 0");

        return Ok(lesson);
    }

    // 需要授权的接口示例
    [HttpGet("secret")]
    [Authorize]
    public ActionResult<string> Secret()
    {
        return "this is secret";
    }

    // 复合查询示例：参数为对象，包含 List<int>，使用 [FromQuery] 绑定并在控制器中验证
    [HttpGet("complex")]
    public IActionResult Complex([FromQuery] ComplexQuery query)
    {
        if (query == null) return BadRequest(DefaultApiResult.Error("Query is required"));

        if (string.IsNullOrWhiteSpace(query.Name)) return BadRequest(DefaultApiResult.Error("Name is required"));
        if (query.Id <= 0) return BadRequest(DefaultApiResult.Error("Id must be greater than 0"));
        if (query.Items == null || query.Items.Count == 0) return BadRequest(DefaultApiResult.Error("Items is required"));
        if (query.Items.Any(i => i <= 0)) return BadRequest(DefaultApiResult.Error("Each item must be greater than 0"));

        return Ok(DefaultApiResult.Success(query));
    }

    // ----------------------- 新增示例接口结束 -----------------------

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

/// <summary>
/// 复合查询 DTO
/// </summary>
public class ComplexQuery
{
    public string Name { get; set; }
    public int Id { get; set; }
    public List<int> Items { get; set; }
}
