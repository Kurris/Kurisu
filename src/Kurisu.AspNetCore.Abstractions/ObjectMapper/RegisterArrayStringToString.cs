
using Mapster;
using Newtonsoft.Json;

namespace Kurisu.AspNetCore.Abstractions.ObjectMapper;

/// <summary>
/// 默认注册
/// </summary>
public class RegisterArrayStringToString : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<List<string>, string>()
            .MapWith(src => src == null || src.Count == 0
                ? "[]"
                : JsonConvert.SerializeObject(src));
    }
}