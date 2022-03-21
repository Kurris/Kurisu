using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Kurisu.Elasticsearch;
using Kurisu.Elasticsearch.Extensions;
using Kurisu.Elasticsearch.Implements;
using Kurisu.Elasticsearch.LowLinq;
using Mapster;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var service = new ElasticSearchService();
            //var lis = await service.PostSearch<Users>()
            //    .OrderByDescending(x => new { x.Age })
            //    .ThenByDescending(x => x.No)
            //    .Where(x => x.Name == "ligy")
            //    .Where(x => x.Age == 25)
            //    .Where(x => x.Address.Contains("杭州") || x.Name == "wei")
            //    .Where(x => x.No == 1)
            //    .ToListAsync();

            var lis =await service.PostSearch<Users>()            
               .Where(x => x.Name.Contains("ligy") || x.Name.Contains("xiao"))
               .ToListAsync();
              
          
            //var  lis = await service.PostSearch<Users>()
            //     .Where(x => x.Name == "ligy" || x.Age == 25 || x.Address.Contains("杭州"))
            //    .ToListAsync();  
        }
    }

    class A
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ADto MyProperty { get; set; }
    }


    public class Users
    {
        public int No { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Address { get; set; }
        public string Job { get; set; }
    }

    class ADto
    {
        public int IdD { get; set; }
        public string Name { get; set; }

        public TTTTT Em { get; set; }

        public override string ToString() => string.Join(',', this.GetType().GetProperties().Select(x => x.Name + " " + x.GetValue(this)));
    }


    enum TTTTT
    {
        aaaa = 0,
        bbbb = 1
    }
}