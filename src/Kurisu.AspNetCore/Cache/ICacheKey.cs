using System;

namespace Kurisu.AspNetCore.Cache;

public interface ICacheKey
{
    public string GetCacheKey(IServiceProvider serviceProvider);
}
