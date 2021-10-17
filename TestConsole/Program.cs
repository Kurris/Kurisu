using System;
using System.Linq;
using Mapster;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //无需配置直接使用 Adapt 扩展方法
            var a = new A
            {
                Id = 1,
                Name = "ligy"
            };

            var aDto = a.BuildAdapter().AddParameters("user", new[] {"ligy"}).AdaptToType<ADto>();
            var config = TypeAdapterConfig<A, ADto>.NewConfig().AddDestinationTransform((string a) => a.Trim());

            Console.WriteLine(aDto.ToString());
        }
    }

    class A
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    class ADto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString() => string.Join(',', this.GetType().GetProperties().Select(x => x.Name + " " + x.GetValue(this)));
    }
}