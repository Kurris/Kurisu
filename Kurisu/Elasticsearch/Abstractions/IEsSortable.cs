using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kurisu.Elasticsearch.LowLinq.Syntax;

namespace Kurisu.Elasticsearch.Abstractions
{
    internal interface IEsSortable
    {
        /// <summary>
        /// 排序参数
        /// </summary>
        IDictionary<string, object> Orders { get; set; }
    }
}
