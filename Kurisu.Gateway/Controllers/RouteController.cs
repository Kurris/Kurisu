using System.Threading.Tasks;
using Kurisu.Gateway.Dto.Input;
using Kurisu.Gateway.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kurisu.Gateway.Controllers
{
    /// <summary>
    /// 路由
    /// </summary>
    [Authorize(Policy = "admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class RouteController : ControllerBase
    {
        private readonly IRouteService _routeService;

        public RouteController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        [HttpPost]
        public async Task SaveRoute(SaveRouteInput input)
        {
            await _routeService.SaveRoute(input);
        }
    }
}