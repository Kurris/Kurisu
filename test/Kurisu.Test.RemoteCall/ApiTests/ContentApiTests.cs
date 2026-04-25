using Kurisu.Test.RemoteCall.TestHelpers;
using Kurisu.Test.RemoteCall.Api;
using Xunit;

namespace Kurisu.Test.RemoteCall.ApiTests;

public class ContentApiTests : IClassFixture<RemoteCallTestFixture>
{
    private readonly RemoteCallTestFixture _fixture;
    public ContentApiTests(RemoteCallTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task PostJsonAsync_SendsJsonBody()
    {
        var api = _fixture.GetService<IContentApi>();
        var result = await api.PostJsonAsync(new { data = "x" });
        Assert.Contains("\"data\":\"x\"", result);
    }

    [Fact]
    public async Task PostFormLikeAsync_EncodesAsKeyValueString()
    {
        var api = _fixture.GetService<IContentApi>();
        var result = await api.PostFormLikeAsync("n1", 5);
        // asUrlencodedFormat true but contentType stays application/json default, body should be key=value&... string
        Assert.Contains("name=n1", result);
        Assert.Contains("qty=5", result);
    }

    [Fact]
    public async Task PostFormRealAsync_UsesFormUrlEncodedContent()
    {
        var api = _fixture.GetService<IContentApi>();
        var result = await api.PostFormRealAsync("n2", 6);
        Assert.Contains("name=n2", result);
        Assert.Contains("qty=6", result);
    }
}

