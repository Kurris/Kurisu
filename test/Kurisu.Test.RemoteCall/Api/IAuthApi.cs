using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Attributes.HelpMethods;
using Kurisu.Test.RemoteCall.MockPolicies;
using Kurisu.Test.RemoteCall.TestHelpers;

namespace Kurisu.Test.RemoteCall.Api;

[EnableRemoteClient("auth-client", "http://localhost:5003", PolicyHandler = typeof(MockAuthPolicy))]
public interface IAuthApi
{
    [RequestAuthorize(typeof(TestAuthTokenHandler))]
    [Get("api/auth/check")]
    Task<string> CheckAsync();
}

