using Kurisu.RemoteCall.Attributes;
using Kurisu.Test.RemoteCall.MockPolicies;
using Kurisu.Test.RemoteCall.Models;

namespace Kurisu.Test.RemoteCall.Api;

[EnableRemoteClient("get-client", "http://localhost:5000", PolicyHandler = typeof(MockGetHttpClientPolicy))]
public interface IGetApi
{
    [Get("api/get1")]
    Task<string> GetTestAsync(string name, int type);

    [Get("api/get2")]
    Task<string> GetTestAsync([RequestQuery] NameAndTypeModel model);
}