using Microsoft.AspNetCore.Mvc;

namespace WebApiDemo.Controllers
{
    /// <summary>
    /// 没有定义ApiDefinition的接口
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class NoApiDefinitionController : ControllerBase
    {
        /// <summary>
        /// 做一些事情
        /// </summary>
        /// <returns>内容</returns>
        [HttpGet("doSomething")]
        public string DoSomething()
        {
            return "DoSomething";
        }
    }
}