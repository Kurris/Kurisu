using Kurisu.Test.RemoteCall.TestHelpers;
using Kurisu.Test.RemoteCall.Api;
using Xunit;

namespace Kurisu.Test.RemoteCall.ApiTests;

public class CustomContentTests : IClassFixture<RemoteCallTestFixture>
{
    private readonly RemoteCallTestFixture _fixture;
    public CustomContentTests(RemoteCallTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task UploadAsync_UsesCustomContentHandler()
    {
        var api = _fixture.GetService<ICustomContentApi>();
        var result = await api.UploadAsync(new { name = "abc" });
        Assert.Contains("custom:", result);
        Assert.Contains("\"name\":\"abc\"", result);
    }
}

