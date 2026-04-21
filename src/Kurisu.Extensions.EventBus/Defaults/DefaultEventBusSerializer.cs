using Kurisu.Extensions.EventBus.Abstractions;
using Newtonsoft.Json;

namespace Kurisu.Extensions.EventBus.Defaults;

public class DefaultEventBusSerializer : IEventBusSerializer
{
    private readonly JsonSerializerSettings _setting = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto,
        DateFormatString = "yyyy-MM-dd HH:mm:ss"
    };

    public string Serialize<TMessage>(TMessage message)
    {
        return JsonConvert.SerializeObject(message, _setting);
    }

    public TMessage Deserialize<TMessage>(string message)
    {
        return JsonConvert.DeserializeObject<TMessage>(message, _setting);
    }
}
