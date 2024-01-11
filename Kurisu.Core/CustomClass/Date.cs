// using Newtonsoft.Json;
//
// namespace Kurisu.Core.CustomClass;
//
// /// <summary>
// /// 日期
// /// </summary>
// [Obsolete]
// [JsonConverter(typeof(DateJsonConverter))]
// public struct Date
// {
//     private readonly DateTime? _dateTime;
//
//     public Date(DateTime dateTime)
//     {
//         _dateTime = dateTime;
//     }
//
//     public Date(DateTime? dateTime)
//     {
//         _dateTime = dateTime;
//     }
//
//     public static Date Now { get; set; }
//
//     public DateTime? DateTime => _dateTime;
//
//     public static implicit operator Date(DateTime d) => new Date(d);
//     public static implicit operator Date(DateTime? d) => new Date(d);
//
//
//     public override string ToString()
//     {
//         return _dateTime?.ToString("yyyy-MM-dd");
//     }
//
//     public string ToString(string format)
//     {
//         return _dateTime?.ToString(format);
//     }
// }
//
// public class DateJsonConverter : JsonConverter
// {
//     public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
//     {
//         writer.WriteValue(value?.ToString() ?? "");
//     }
//
//     public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
//     {
//         throw new NotImplementedException();
//     }
//
//     public override bool CanConvert(Type objectType)
//     {
//         return objectType == typeof(Date);
//     }
// }