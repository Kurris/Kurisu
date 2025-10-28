namespace Kurisu.RemoteCall.Abstractions;

public interface IJsonSerializer
{
    string Serialize(object obj);
    T Deserialize<T>(string json);
    object Deserialize(string json, System.Type type);
}