using Kurisu.Test.RemoteCall.TestHelpers;
using Kurisu.Test.RemoteCall.Api;
using Kurisu.Test.RemoteCall.Models;
using Xunit;

namespace Kurisu.Test.RemoteCall.ApiTests;

public class GetApiTests : IClassFixture<RemoteCallTestFixture>
{
    private readonly RemoteCallTestFixture _fixture;

    public GetApiTests(RemoteCallTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetTestAsync_WithPrimitiveParameters_ReturnsExpectedJson()
    {
        var api = _fixture.GetService<IGetApi>();
        var name = "ligy";
        var type = 1;

        var result = await api.GetTestAsync(name, type);

        Assert.Equal(new { name, type }.ToJson(), result);
    }

    [Fact]
    public async Task GetTestAsync_WithModelAsQuery_ReturnsExpectedJson()
    {
        var api = _fixture.GetService<IGetApi>();
        var model = new NameAndTypeModel { Name = "ligy", Type = 1 };

        var result = await api.GetTestAsync(model);

        Assert.Equal(model.ToJson(), result);
    }

    // New edge-case tests
    [Fact]
    public async Task GetTestAsync_WithSpecialCharacters_ReturnsDecodedValues()
    {
        var api = _fixture.GetService<IGetApi>();
        var name = "a b&c=\"x\""; // includes space, ampersand and quotes
        var type = 2;

        var result = await api.GetTestAsync(name, type);

        Assert.Equal(new { name, type }.ToJson(), result);
    }

    [Fact]
    public async Task GetTestAsync_WithLargeNumber_ReturnsExpectedJson()
    {
        var api = _fixture.GetService<IGetApi>();
        var name = "big";
        var type = 999999999;

        var result = await api.GetTestAsync(name, type);

        Assert.Equal(new { name, type }.ToJson(), result);
    }

    [Fact]
    public async Task GetTestAsync_ModelPropertyCasing_PreservedInQuery()
    {
        var api = _fixture.GetService<IGetApi>();
        var model = new NameAndTypeModel { Name = "Pascal", Type = 5 };

        var result = await api.GetTestAsync(model);

        // The mock expects query keys named "Name" and "Type", and returns JSON with PascalCase keys
        Assert.Equal(model.ToJson(), result);
    }
}
