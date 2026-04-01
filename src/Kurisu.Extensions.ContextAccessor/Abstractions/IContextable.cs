using Newtonsoft.Json;

namespace Kurisu.Extensions.ContextAccessor.Abstractions;

/// <summary>
/// 定义上下文
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IContextable<out T> where T : new()
{
    public T CopyState()
    {
        var json = JsonConvert.SerializeObject(this);
        return JsonConvert.DeserializeObject<T>(json);
    }
}