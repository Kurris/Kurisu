using Kurisu.Startup;

namespace Kurisu.Test.WebApi_A;

class Program
{
    public static void Main(string[] args)
    {
        KurisuHost.Run<Startup>(true, args);
    }
}