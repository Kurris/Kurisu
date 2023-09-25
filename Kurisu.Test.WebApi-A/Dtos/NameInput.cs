using System.ComponentModel.DataAnnotations;
using Kurisu.DataAccess.Dto;

namespace Kurisu.Test.WebApi_A.Dtos;

public class NameInput : PageInput
{
    [Required(ErrorMessage = "Name不能为空")]
    public string Name { get; set; }
}