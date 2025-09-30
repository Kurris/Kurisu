using System.ComponentModel.DataAnnotations;
using Kurisu.AspNetCore.Utils.Converters;
using Newtonsoft.Json;

namespace Kurisu.Test.WebApi_A
{
    public class FaceResponse
    {
        [Required]
        public string Name { get; set; }
        
        [JsonConverter(typeof(ToDateJsonConverter))]
        public DateTime? Face { get; set; }
    }
}
