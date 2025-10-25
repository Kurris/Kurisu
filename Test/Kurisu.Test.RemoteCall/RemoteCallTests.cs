using Kurisu.Test.RemoteCall.Api;
using Kurisu.Test.RemoteCall.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.RemoteCall;

public class RemoteCallTests
{
    private static readonly IServiceProvider ServiceProvider;

    static RemoteCallTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddRemoteCall(new[] { typeof(IGetApi), typeof(IPostApi), typeof(IWeatherApi) });

        ServiceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetTestAsync_ShouldReturnExpectedResult()
    {
        var api = ServiceProvider.GetRequiredService<IGetApi>();

        var queryObj = new
        {
            name = "ligy",
            type = 1
        };

        var result = await api.GetTestAsync(queryObj.name, queryObj.type);
        Assert.Equal(queryObj.ToJson(), result);
    }

    [Fact]
    public async Task PostTestAsync_ShouldReturnExpectedResult()
    {
        var api = ServiceProvider.GetRequiredService<IGetApi>();
        var model = new NameAndTypeModel
        {
            Name = "ligy",
            Type = 1
        };

        var result = await api.GetTestAsync(model);
        Assert.Equal(model.ToJson(), result);
    }

    [Fact]
    public async Task PostTestAsync_ShouldPostDataSuccessfully()
    {
        var api = ServiceProvider.GetRequiredService<IPostApi>();
        var result = await api.PostTestAsync("ligy");
        Assert.Equal(new { data = "ligy" }.ToJson(), result);
    }

    [Fact]
    public async Task Weather_Ping_ShouldReturnPong()
    {
        var api = ServiceProvider.GetRequiredService<IWeatherApi>();
        var result = await api.PingAsync();
        Assert.Equal("pong", result.Trim('"'));
    }

    [Fact]
    public async Task Weather_Echo_ShouldReturnValue()
    {
        var api = ServiceProvider.GetRequiredService<IWeatherApi>();
        var result = await api.EchoAsync(123);
        Assert.Equal("123", result.Trim('"'));
    }

    [Fact]
    public async Task Weather_Create_ShouldEchoModel()
    {
        var api = ServiceProvider.GetRequiredService<IWeatherApi>();
        var model = new TestResult { Url = "/api/weather/1", UserName = "user1" };
        var result = await api.CreateAsync(model);
        Assert.Contains("user1", result);
    }

    [Fact]
    public async Task Weather_List_ShouldReturnJsonArray()
    {
        var api = ServiceProvider.GetRequiredService<IWeatherApi>();
        var result = await api.GetListAsync();
        Assert.StartsWith("[", result.Trim());
    }

    [Fact]
    public async Task Weather_Complex_ShouldReturnParsedQuery()
    {
        var api = ServiceProvider.GetRequiredService<IWeatherApi>();
        var query = new Kurisu.Test.RemoteCall.Models.ComplexQuery
        {
            Name = "abc",
            Id = 5,
            Items = new List<int> { 1, 2, 3 }
        };

        var result = await api.ComplexAsync(query);
        // result is JSON like {"Name":"abc","Id":5,"Items":[1,2,3]}
        Assert.Contains("\"Name\":\"abc\"", result);
        Assert.Contains("\"Id\":5", result);
        Assert.Contains("\"Items\":", result);
    }
}