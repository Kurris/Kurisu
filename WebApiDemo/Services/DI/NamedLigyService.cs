using Microsoft.Extensions.DependencyInjection;

namespace WebApiDemo.Services.DI
{
    [Register("ligy")]
    public class NamedLigyService : INamedService
    {
        public string Hello()
        {
            return "I'm ligy";
        }
    }
}