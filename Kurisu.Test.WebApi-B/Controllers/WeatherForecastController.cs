//using Kurisu.AspNetCore.Grpc.Abstractions;
//using Kurisu.AspNetCore.MVC;
//using Kurisu.Test.Greet;
//using Kurisu.Test.WebApi_B.Dtos;
//using Kurisu.Test.WebApi_B.GrpcModules;
//using Microsoft.AspNetCore.Mvc;
//using static Kurisu.Test.Greet.Greeter;

//namespace Kurisu.Test.WebApi_B.Controllers;

//[ApiDefinition("天气")]
//[ApiController]
//[Route("api/[controller]")]
//public class WeatherForecastController : ControllerBase
//{
//    private static readonly string[] Summaries = new[]
//    {
//        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//    };

//    private readonly ILogger<WeatherForecastController> _logger;
//    //private readonly IGrpcClientService<AGrpcModule, GreeterClient> _grpcClientService;

//    public WeatherForecastController(ILogger<WeatherForecastController> logger)
//    {
//        _logger = logger;
//        _grpcClientService = grpcClientService;
//    }

//    [HttpGet]
//    public async Task<object> GetWeatherForecast([FromQuery] NameInput input)
//    {
//        var result = await _grpcClientService.Instance.SayHelloAsync(new HelloRequest
//        {
//            Name = input.Name
//        });

//        return result;
//    }
//}