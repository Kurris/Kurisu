using Kurisu.Core.Proxy.Attributes;
using Kurisu.SqlSugar.Aops;

namespace Kurisu.SqlSugar.Attributes;

public class TransactionalAttribute : AopAttribute
{
    public TransactionalAttribute()
    {
        if (Interceptors?.Any() != true)
        {
            Interceptors = new[] { typeof(Transactional) };
        }
    }
}