
using Mapster;
using Newtonsoft.Json;

namespace Kurisu.AspNetCore.Abstractions.ObjectMapper;

/// <summary>
/// 默认注册
/// </summary>
public class RegisterStringToArrayString : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<string, List<string>>().MapWith(src => string.IsNullOrWhiteSpace(src)
            ? new List<string>()
            : JsonConvert.DeserializeObject<List<string>>(src));
    }
}