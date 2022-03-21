using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Kurisu.Common.Dto;

namespace Kurisu.Elasticsearch.LowLinq.Syntax
{
    internal class EsPage
    {
        internal Dictionary<string, int> Build(PageIn pageIn)
        {
            return new Dictionary<string, int>()
            {
                ["from"] = (pageIn.PageIndex - 1) * pageIn.PageSize,
                ["size"] = pageIn.PageSize,
            };
        }
    }
}
