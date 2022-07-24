using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kurisu.Gateway
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args).RunKurisuAsync<Startup>();
        }
    }
}
