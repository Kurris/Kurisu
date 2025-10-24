using Kurisu.RemoteCall.Attributes;

namespace Kurisu.Api;

[EnableRemoteClient("Kurisu.Api", "${a-service}")]
public interface IApi
{
}