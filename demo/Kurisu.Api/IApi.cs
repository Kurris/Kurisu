using Kurisu.RemoteCall.Attributes;

namespace Kurisu.Api;

[EnableRemoteClient("Kurisu.Api", "${a-service}", SettingFile = "api.json")]
public interface IApi
{
}