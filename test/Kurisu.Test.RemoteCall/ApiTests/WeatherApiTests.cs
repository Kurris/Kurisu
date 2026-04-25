using Kurisu.Test.RemoteCall.TestHelpers;
using Kurisu.Test.RemoteCall.Api;
using Kurisu.Test.RemoteCall.Models;

namespace Kurisu.Test.RemoteCall.ApiTests;

public class WeatherApiTests : IClassFixture<RemoteCallTestFixture>
{
    private readonly RemoteCallTestFixture _fixture;

    public WeatherApiTests(RemoteCallTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PingAsync_ReturnsPong()
    {
        var api = _fixture.GetService<IWeatherApi>();
        var result = await api.PingAsync();
        Assert.Equal("pong", result.Trim('"'));
    }

    [Fact]
    public async Task EchoAsync_ReturnsValue()
    {
        var api = _fixture.GetService<IWeatherApi>();
        var result = await api.EchoAsync(123);
        Assert.Equal("123", result.Trim('"'));
    }

    [Fact]
    public async Task CreateAsync_EchoesModel()
    {
        var api = _fixture.GetService<IWeatherApi>();
        var model = new TestResult { Url = "/api/weather/1", UserName = "user1" };
        var result = await api.CreateAsync(model);
        Assert.Contains("user1", result);
    }

    [Fact]
    public async Task GetListAsync_ReturnsJsonArray()
    {
        var api = _fixture.GetService<IWeatherApi>();
        var result = await api.GetListAsync();
        Assert.StartsWith("[", result.Trim());
    }

    [Fact]
    public async Task ComplexAsync_ReturnsParsedQuery()
    {
        var api = _fixture.GetService<IWeatherApi>();
        var query = new ComplexQuery { Name = "abc", Id = 5, Items = new List<int> { 1, 2, 3 } };
        var result = await api.ComplexAsync(query);
        Assert.Contains("\"Name\":\"abc\"", result);
        Assert.Contains("\"Id\":5", result);
        Assert.Contains("\"Items\":", result);
    }
}
