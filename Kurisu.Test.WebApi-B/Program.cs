using Kurisu.Startup;

namespace Kurisu.Test.WebApi_B;

class Program
{
    public static void Main(string[] args)
    {
        KurisuHost.Run<Startup>(true, args);
    }
}