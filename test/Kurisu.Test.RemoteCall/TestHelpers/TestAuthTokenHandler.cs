using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.Test.RemoteCall.TestHelpers;

public class TestAuthTokenHandler : IRemoteCallAuthTokenHandler
{
    public Task<string> GetTokenAsync()
    {
        return Task.FromResult("Bearer test-token-123");
    }
}

