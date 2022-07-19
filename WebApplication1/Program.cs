using System.Threading.Tasks;
using Kurisu.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace WebApplication1
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args).RunKurisuAsync<Startup>();
        }
    }
}