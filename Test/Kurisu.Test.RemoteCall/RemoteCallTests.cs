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
        services.AddRemoteCall(new[] { typeof(IGetApi) ,typeof(IPostApi)});

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
        // Act
        var result = await api.GetTestAsync(model);

        // Assert
        Assert.Equal(model.ToJson(), result);
    }

    [Fact]
    public async Task PostTestAsync_ShouldPostDataSuccessfully()
    {
        var api = ServiceProvider.GetRequiredService<IPostApi>();

        var result = await api.PostTestAsync("ligy");

        Assert.Equal(new { data = "ligy" }.ToJson(), result);
    }
}