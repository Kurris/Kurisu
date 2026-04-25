using Kurisu.Test.RemoteCall.TestHelpers;
using Kurisu.Test.RemoteCall.Api;

namespace Kurisu.Test.RemoteCall.ApiTests;

public class PostApiTests : IClassFixture<RemoteCallTestFixture>
{
    private readonly RemoteCallTestFixture _fixture;

    public PostApiTests(RemoteCallTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostTestAsync_WithData_ReturnsPostedJson()
    {
        var api = _fixture.GetService<IPostApi>();
        var data = "ligy";

        var result = await api.PostTestAsync(data);

        Assert.Equal(new { data }.ToJson(), result);
    }
}
