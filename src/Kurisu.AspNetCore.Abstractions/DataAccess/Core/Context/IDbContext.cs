using Kurisu.AspNetCore.Abstractions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

/// <summary>
/// 数据库上下文
/// </summary>
[SkipScan]
public interface IDbContext : IFilterOperator, IReadDbContext, IWriteDbContext;