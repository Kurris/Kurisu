using Kurisu.RemoteCall.Attributes;

namespace Kurisu.Test.WebApi_A;


[Auth(TokenHandler = typeof(JipTokenHandler))]
[EnableRemoteClient(Name = "jip-service", BaseUrl = "http://113.195.134.120:8000")]
public interface IJipApi
{
    [Post("/JIP/DHR/DHR_DL_CALLBACK_UAT")]
    Task<JipResponse> CallbackAsync(List<TestRequest> requests);
}
