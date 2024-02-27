using System.ComponentModel.DataAnnotations;

namespace Kurisu.Test.WebApi_A
{
    public class TestRequest
    {
        [Required]
        public string Name { get; set; }
    }
}
