using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kurisu.DataAccessor;

internal class CustomModelCacheKey : ModelCacheKey
{
    private readonly Type _contextType;
    private readonly int? _key;
    public CustomModelCacheKey(int? key, DbContext context) : base(context)
    {
        _key = key;
        _contextType = context.GetType();
    }

    public virtual bool Equals(CustomModelCacheKey other) => _contextType == other._contextType && _key == other._key;

    public override bool Equals(object obj) => (obj is CustomModelCacheKey otherAsKey) && Equals(otherAsKey);


    public override int GetHashCode() => _key.GetHashCode();
}