using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class NoApiController : ControllerBase
    {
        [HttpGet("T112")]
        public string GetT()
        {
            return "GetT333";
        }
    }
}