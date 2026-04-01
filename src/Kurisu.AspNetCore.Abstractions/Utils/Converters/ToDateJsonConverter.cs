using System;
using Newtonsoft.Json;

namespace Kurisu.AspNetCore.Utils.Converters;

/// <summary>
/// 日期转换
/// </summary>
public class ToDateJsonConverter : JsonConverter
{
    private readonly string _defaultFormat;

    /// <summary>
    /// ctor
    /// </summary>
    public ToDateJsonConverter()
    {
        _defaultFormat = "yyyy-MM-dd";
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="format"></param>
    public ToDateJsonConverter(string format)
    {
        _defaultFormat = format;
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
            writer.WriteValue(dateTime.ToString(_defaultFormat));
        }
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return reader.Value;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateTime?) || objectType == typeof(DateTime);
    }
}