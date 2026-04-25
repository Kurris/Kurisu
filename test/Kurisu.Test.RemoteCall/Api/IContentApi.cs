using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Attributes.HelpMethods;
using Kurisu.Test.RemoteCall.MockPolicies;

namespace Kurisu.Test.RemoteCall.Api;

[EnableRemoteClient("content-client", "http://localhost:5004", PolicyHandler = typeof(MockContentPolicy))]
public interface IContentApi
{
    [Post("api/content/json")]
    Task<string> PostJsonAsync([RequestBody] object model);

    [Post("api/content/form", asUrlencodedFormat: true)]
    Task<string> PostFormLikeAsync(string name, int qty);

    [Post("api/content/formreal", contentType: "application/x-www-form-urlencoded", asUrlencodedFormat: true)]
    Task<string> PostFormRealAsync(string name, int qty);
}

