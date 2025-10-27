using Kurisu.RemoteCall.Attributes;
using Kurisu.Test.RemoteCall.MockPolicies;
using Kurisu.Test.RemoteCall.TestHelpers;

namespace Kurisu.Test.RemoteCall.Api;

[EnableRemoteClient("resp-client", "http://localhost:5005", PolicyHandler = typeof(MockResponsePolicy))]
[ResponseResult(typeof(TestRemoteCallResultHandler))]
public interface IResponseApi
{
    [Get("api/resp/value")]
    Task<string> GetValueAsync();
}

