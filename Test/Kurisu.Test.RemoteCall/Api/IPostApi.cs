using System.Text;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Kurisu.Test.RemoteCall.MockPolicies;

namespace Kurisu.Test.RemoteCall.Api;

[EnableRemoteClient("post-client", "http://localhost:5000", PolicyHandler = typeof(MockPostHttpClientPolicy))]
public interface IPostApi
{
    [RequestContent(typeof(PostContentHandler))]
    [Post("api/post1", "application/x-www-form-urlencoded")]
    Task<string> PostTestAsync(string data);
}

public class PostContentHandler : IRemoteCallContentHandler
{
    public HttpContent PostTestAsync(string data)
    {
        return new StringContent(new { data }.ToJson(), Encoding.UTF8, "application/json");
    }
}