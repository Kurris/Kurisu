using Microsoft.Extensions.DependencyInjection;

namespace WebApiDemo.Services.DI
{
    [Register("xiao")]
    public class NamedXiaoService : INamedService
    {
        public string Hello()
        {
            return "I'm xiao";
        }
    }
}