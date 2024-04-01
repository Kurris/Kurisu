using System;
using System.Linq;
using Newtonsoft.Json;

namespace Kurisu.AspNetCore.Utils.Converters;

/// <summary>
/// 数据混淆
/// </summary>
public class ObfuscateJsonConverter : JsonConverter
{
    private readonly int _last;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="last">保留尾部</param>
    public ObfuscateJsonConverter(int last)
    {
        _last = last;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var v = value?.ToString() ?? string.Empty;
        var vc = v.ToCharArray().Reverse().ToList();

        for (int i = 0; i < vc.Count; i++)
        {
            if (i + 1 > _last)
            {
                vc[i] = '*';
            }
        }
        vc.Reverse();
        v = string.Join("", vc);

        writer.WriteValue(v);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string);
    }
}
