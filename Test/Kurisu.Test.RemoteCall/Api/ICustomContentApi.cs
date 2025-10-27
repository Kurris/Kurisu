using Kurisu.RemoteCall.Attributes;
using Kurisu.Test.RemoteCall.MockPolicies;
using Kurisu.Test.RemoteCall.TestHelpers;

namespace Kurisu.Test.RemoteCall.Api;

[EnableRemoteClient("custom-content-client", "http://localhost:5006", PolicyHandler = typeof(MockCustomContentPolicy))]
public interface ICustomContentApi
{
    [RequestContent(typeof(CustomContentHandler))]
    [Post("api/custom/upload")]
    Task<string> UploadAsync(object model);
}
