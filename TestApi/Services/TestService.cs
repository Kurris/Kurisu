using System.Threading.Tasks;
using Kurisu.DependencyInjection.Attributes;

namespace TestApi.Services
{
    [Register(Proxy = typeof(TestProxy))]
    public class TestService : ITestService
    {
        public string Get()
        {
            return "get string";
        }

        public void Set()
        {
           
        }
    }
}