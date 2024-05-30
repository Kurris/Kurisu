using System;
using Newtonsoft.Json;

namespace Kurisu.AspNetCore.Utils.Converters;

/// <summary>
/// 日期转换
/// </summary>
public class ToDateJsonConverter : JsonConverter
{
    private readonly string _format;

    /// <summary>
    /// ctor
    /// </summary>
    public ToDateJsonConverter()
    {
        _format = "yyyy-MM-dd";
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="format"></param>
    public ToDateJsonConverter(string format)
    {
        _format = format;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteValue(string.Empty);
        }
        else
        {
            DateTime dateTime = Convert.ToDateTime(value);
            writer.WriteValue(dateTime.ToString(_format));
        }
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateTime?) || objectType == typeof(DateTime);
    }
}