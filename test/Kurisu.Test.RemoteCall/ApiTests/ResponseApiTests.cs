using Kurisu.Test.RemoteCall.TestHelpers;
using Kurisu.Test.RemoteCall.Api;
using Xunit;

namespace Kurisu.Test.RemoteCall.ApiTests;

public class ResponseApiTests : IClassFixture<RemoteCallTestFixture>
{
    private readonly RemoteCallTestFixture _fixture;
    public ResponseApiTests(RemoteCallTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetValueAsync_UsesCustomResultHandler()
    {
        var api = _fixture.GetService<IResponseApi>();
        var result = await api.GetValueAsync();
        Assert.Equal("hello", result);
    }
}

