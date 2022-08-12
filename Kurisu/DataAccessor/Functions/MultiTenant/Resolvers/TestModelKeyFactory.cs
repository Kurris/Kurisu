using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kurisu.DataAccessor.Functions.MultiTenant.Resolvers
{
    public class TestModelKeyFactory<TDbContext> : ModelCacheKeyFactory where TDbContext : DbContext
    {
        public TestModelKeyFactory(ModelCacheKeyFactoryDependencies dependencies) : base(dependencies)
        {
        }

        public override object Create(DbContext context)
        {
            return new TestModelKey<TDbContext>(context as TDbContext);
        }
    }


    public class TestModelKey<TDbContext> : ModelCacheKey where TDbContext : DbContext
    {
        private readonly TDbContext _context;

        public TestModelKey(TDbContext context) : base(context)
        {
            _context = context;
        }

        protected override bool Equals(ModelCacheKey other)
        {
            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode();
            return hashCode;
        }
    }
}