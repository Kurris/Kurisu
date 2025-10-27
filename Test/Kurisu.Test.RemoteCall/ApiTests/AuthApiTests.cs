using Kurisu.Test.RemoteCall.TestHelpers;
using Kurisu.Test.RemoteCall.Api;
using Xunit;

namespace Kurisu.Test.RemoteCall.ApiTests;

public class AuthApiTests : IClassFixture<RemoteCallTestFixture>
{
    private readonly RemoteCallTestFixture _fixture;
    public AuthApiTests(RemoteCallTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CheckAsync_IncludesAuthorizationHeader()
    {
        var api = _fixture.GetService<IAuthApi>();
        var result = await api.CheckAsync();
        Assert.Contains("Bearer test-token-123", result);
    }
}

