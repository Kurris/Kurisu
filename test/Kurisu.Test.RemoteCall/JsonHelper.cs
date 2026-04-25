using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.Test.RemoteCall;

public static class JsonHelper
{
    public static JsonSerializerSettings CamelCase = new JsonSerializerSettings()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public static string ToJson(this object obj)
    {
        return JsonConvert.SerializeObject(obj);
    }


    public static string ToJson(this object obj, JsonSerializerSettings settings)
    {
        return JsonConvert.SerializeObject(obj, settings);
    }
}