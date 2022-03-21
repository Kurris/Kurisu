using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Kurisu.Elasticsearch.LowLinq.Syntax
{
    internal class EsSort
    {
        public EsSort(EsSortType esSortType)
        {
            if (esSortType == EsSortType.ASC)
                Order = "asc";
            else
                Order = "desc";
        }

        [JsonProperty("order")]
        public string Order { get; set; }
    }

    internal enum EsSortType
    {
        ASC = 0,
        DESC = 1,
    }
}
