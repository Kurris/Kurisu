using System.ComponentModel.DataAnnotations;

namespace Kurisu.Test.WebApi_B.Dtos;

public class NameInput : IValidatableObject
{
    [Required(ErrorMessage = "Name不能为空")] public string Name { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name == "ligy")
        {
            yield return new ValidationResult("名称不能为ligy", new[] {nameof(Name)});
        }
    }
}