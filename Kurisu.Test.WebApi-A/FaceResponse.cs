using Kurisu.AspNetCore.Utils.Converters;
using Newtonsoft.Json;

namespace Kurisu.Test.WebApi_A
{
    public class FaceResponse
    {

        [JsonConverter(typeof(ToDateJsonConverter))]
        public DateTime? Face { get; set; }
    }
}
