using System;
using Newtonsoft.Json;

namespace Kurisu.AspNetCore.CustomClass;

/// <summary>
/// 日期
/// </summary>
[JsonConverter(typeof(DateJsonConverter))]
public struct Date
{
    private readonly DateTime? _dateTime;

    /// <summary>
    /// 日期
    /// </summary>
    /// <param name="dateTime"></param>
    public Date(DateTime dateTime)
    {
        _dateTime = dateTime;
    }

    /// <summary>
    /// 日期
    /// </summary>
    /// <param name="dateTime"></param>
    public Date(DateTime? dateTime)
    {
        _dateTime = dateTime;
    }

    /// <summary>
    /// 今天
    /// </summary>
    public static Date Today => DateTime.Today;

    /// <summary>
    /// 隐私转换
    /// </summary>
    /// <param name="d"></param>
    public static implicit operator Date(DateTime d) => new(d);

    /// <summary>
    /// 隐私转换
    /// </summary>
    /// <param name="d"></param>
    public static implicit operator Date(DateTime? d) => new(d);


    /// <summary>
    /// Date Format
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return _dateTime?.ToString("yyyy-MM-dd");
    }


    private class DateJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString() ?? string.Empty);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Date);
        }
    }
}