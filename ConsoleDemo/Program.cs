using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient();

            var result = new ConcurrentBag<int>();

            async void Body(int i)
            {
                await httpClient.GetStringAsync("http://localhost:5003/DbContext/doSomething");
                result.Add(1);
                Console.WriteLine(result.Count);
            }

            Parallel.For(0, 100, Body);

            Console.WriteLine(result.Sum());

            Console.ReadKey();
        }
    }
}