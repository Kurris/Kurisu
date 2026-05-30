using Kurisu.Extensions.EventBus.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.Extensions.EventBus.Defaults;

/// <summary>
/// 事件消息 JSON 序列化器，序列化时固定使用 EventMessage 基类型以隐藏 ProcessingToken 等运行时字段。
/// 内置 BindToType 白名单校验，仅允许反序列化 EventMessage 子类，防止反序列化注入。
/// </summary>
public class DefaultEventBusSerializer : IEventBusSerializer
{
    private readonly JsonSerializerSettings _setting = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto,
        SerializationBinder = new EventMessageSerializationBinder(),
        DateFormatString = "yyyy-MM-dd HH:mm:ss"
    };

    /// <summary>
    /// 序列化为 JSON，固定使用 EventMessage 基类以忽略派生类运行时字段。
    /// </summary>
    public string Serialize<TMessage>(TMessage message)
    {
        return JsonConvert.SerializeObject(message, typeof(EventMessage), _setting);
    }

    /// <summary>
    /// 反序列化 JSON 为消息对象，经过类型白名单校验。
    /// </summary>
    public TMessage Deserialize<TMessage>(string message)
    {
        return JsonConvert.DeserializeObject<TMessage>(message, _setting);
    }

    /// <summary>
    /// 序列化类型绑定器，仅允许 EventMessage 子类的序列化与反序列化，防止类型注入攻击。
    /// </summary>
    private sealed class EventMessageSerializationBinder : ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            var type = Type.GetType($"{typeName}, {assemblyName}", false);
            if (type is null || !typeof(EventMessage).IsAssignableFrom(type))
            {
                throw new JsonSerializationException($"不允许反序列化事件消息类型: {typeName}");
            }

            return type;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (!typeof(EventMessage).IsAssignableFrom(serializedType))
            {
                throw new JsonSerializationException($"不允许序列化事件消息类型: {serializedType.FullName}");
            }

            assemblyName = serializedType.Assembly.FullName;
            typeName = serializedType.FullName;
        }
    }
}
