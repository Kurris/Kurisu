using System.Linq;
using Kurisu.DataAccess.Functions.Default.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccess.Functions.Default.Internal;

/// <summary>
/// 数据库访问服务
/// </summary>
public class DefaultAppDbService : WriteImplementation, IDbService
{
    private readonly DbContext _dbContext;

    public DefaultAppDbService(IDbWrite dbWrite) : base(dbWrite.GetDbContext())
    {
        _dbContext = dbWrite.GetDbContext();
    }

    public virtual IQueryable<T> AsQueryable<T>(bool useWriteDb) where T : class, new()
    {
        return _dbContext.Set<T>().AsQueryable();
    }
}