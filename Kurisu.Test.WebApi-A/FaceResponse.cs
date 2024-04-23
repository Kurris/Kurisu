using Kurisu.AspNetCore.Utils.Converters;
using Newtonsoft.Json;

namespace Kurisu.Test.WebApi_A
{
    public class FaceResponse
    {

        [JsonConverter(typeof(ObfuscateJsonConverter), 6)]
        public string Face { get; set; }
    }
}
