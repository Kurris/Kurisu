using System;
using System.Reflection;
using System.Threading.Tasks;
using Kurisu.Proxy;
using Kurisu.Proxy.Global;
using DispatchProxy = Kurisu.Proxy.DispatchProxy;

namespace TestApi
{
    public class GlobalProxy : DispatchProxy, IGlobalDispatchProxy
    {
        
    }
}