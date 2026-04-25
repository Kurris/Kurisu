using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Attributes.HelpMethods;
using Kurisu.Test.RemoteCall.MockPolicies;
using Kurisu.Test.RemoteCall.Models;

namespace Kurisu.Test.RemoteCall.Api;

[EnableRemoteClient("weather-client", "http://localhost:5001", PolicyHandler = typeof(MockWeatherHttpClientPolicy))]
public interface IWeatherApi
{
    [Get("api/weather/ping")]
    Task<string> PingAsync();

    [Get("api/weather/echo")]
    Task<string> EchoAsync(int value);

    [Get("api/weather/list")]
    Task<string> GetListAsync();

    [Post("api/weather/create")]
    Task<string> CreateAsync(TestResult model);

    [Get("api/weather/complex")]
    Task<string> ComplexAsync([RequestQuery] ComplexQuery query);
}
