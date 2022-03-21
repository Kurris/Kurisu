using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kurisu.Elasticsearch.Abstractions
{
    public interface ISelectable<TDocument,TResult> where TDocument : class,new() where TResult : class
    {
    }
}
