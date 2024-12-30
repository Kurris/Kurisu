using System.ComponentModel.DataAnnotations;

namespace Kurisu.Test.WebApi_A
{
    public class TestRequest : IValidatableObject
    {
        [Required]
        public string uniqueId { get; set; }
        
        public string status { get; set; }

        public string msg { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}
