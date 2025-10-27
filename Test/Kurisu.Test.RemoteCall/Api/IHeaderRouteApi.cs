using Kurisu.RemoteCall.Attributes;
using Kurisu.Test.RemoteCall.MockPolicies;

namespace Kurisu.Test.RemoteCall.Api;

[EnableRemoteClient("hdr-client", "http://localhost:5002", PolicyHandler = typeof(MockHeaderRoutePolicy))]
public interface IHeaderRouteApi
{
    [Get("api/items/${id}")]
    [RequestHeader("X-Custom", "hvalue")]
    Task<string> GetItemAsync([RequestRoute] int id);
}
