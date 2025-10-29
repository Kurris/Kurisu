namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 序列化器
/// </summary>
public interface IJsonSerializer
{
    /// <summary>
    ///  序列化对象
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    string Serialize(object obj);

    /// <summary>
    ///  反序列化对象
    /// </summary>
    /// <param name="json"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T Deserialize<T>(string json);

    /// <summary>
    ///  反序列化对象
    /// </summary>
    /// <param name="json"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    object Deserialize(string json, System.Type type);
}