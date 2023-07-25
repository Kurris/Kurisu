using System.ComponentModel.DataAnnotations;

namespace Kurisu.Test.WebApi_A.Dtos;

public class NameInput
{
    [Required(ErrorMessage = "Name不能为空")]
    public string Name { get; set; }
}