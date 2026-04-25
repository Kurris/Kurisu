using System.Net.Http;
using System.Text;
using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.Test.RemoteCall.TestHelpers;

public class CustomContentHandler : IRemoteCallContentHandler
{
    public HttpContent UploadAsync(object model)
    {
        // produce a custom body format
        var str = $"custom:{model.ToJson()}";
        return new StringContent(str, Encoding.UTF8, "application/json");
    }
}

