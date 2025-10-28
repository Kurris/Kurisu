// filepath: e:\github\Kurisu\test\Kurisu.Test.RemoteCall\Api\IResponseAltApi.cs
using Kurisu.RemoteCall.Attributes;
using Kurisu.Test.RemoteCall.MockPolicies;
using Kurisu.Test.RemoteCall.TestHelpers;

namespace Kurisu.Test.RemoteCall.Api;

[EnableRemoteClient("resp-client-alt", "http://localhost:5005", PolicyHandler = typeof(MockResponseAltPolicy))]
[ResponseResult(typeof(TestRemoteCallResultHandler))]
public interface IResponseAltApi
{
    [Get("api/resp/alt")]
    Task<string> GetAltAsync();
}

