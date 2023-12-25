using Kurisu.Core.CustomClass;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.Test.WebApi_A.Dtos
{

    public class TestDto
    {
        //[JsonProperty("DATE", PropertyName = "DATE")]
        public DateTime LogDate { get; set; }
    }
}
