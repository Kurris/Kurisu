using Kurisu.Test.RemoteCall.TestHelpers;
using Kurisu.Test.RemoteCall.Api;
using Xunit;

namespace Kurisu.Test.RemoteCall.ApiTests;

public class HeaderRouteTests : IClassFixture<RemoteCallTestFixture>
{
    private readonly RemoteCallTestFixture _fixture;
    public HeaderRouteTests(RemoteCallTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GetItemAsync_IncludesRouteAndHeader()
    {
        var api = _fixture.GetService<IHeaderRouteApi>();
        var result = await api.GetItemAsync(123);
        Assert.Contains("123", result);
        Assert.Contains("hvalue", result);
    }
}
