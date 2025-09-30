using System.ComponentModel.DataAnnotations;
using Kurisu.AspNetCore.DataAccess.Dto;

namespace Kurisu.Test.WebApi_A.Requests
{
    public class SearchDto : PageDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name can't be empty")]
        public string Name { get; set; }
    }
}