using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.ContextAccessor;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Sharding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.SqlSugar;

/// <summary>
/// sqlsugar注入构建器
/// </summary>
public class SqlSugarServiceBuilder
{
    private readonly IServiceCollection _services;

    public SqlSugarServiceBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public bool UseSharding { get; set; }

    /// <summary>
    /// 启用shading功能
    /// </summary>
    /// <returns></returns>
    public SqlSugarServiceBuilder EnableSharding()
    {
        _services.AddMemoryCache();

        _services.Replace(ServiceDescriptor.Describe(typeof(IDbContext), typeof(ShardingContext), ServiceLifetime.Scoped));

        _services.Replace(ServiceDescriptor.Describe(typeof(IContextAccessor<DbOperationState>), typeof(ShardingStateAccessor), ServiceLifetime.Singleton));

        this.UseSharding = true;
        return this;
    }
}


internal class ShardingStateAccessor : AbstractContextAccessor<DbOperationState>
{
    private static readonly AsyncLocal<StateHolder> _stateCurrent = new();

    public ShardingStateAccessor(ILogger<ShardingStateAccessor> logger) : base(logger)
    {
    }

    public override void Initialize()
    {
        base.Initialize();
        Current.IgnoreSharding = false;
    }

    public override DbOperationState Current
    {
        get => _stateCurrent.Value?.State;
        set
        {
            var holder = _stateCurrent.Value;
            if (holder != null)
            {
                holder.State = null;
            }

            if (value != null)
            {
                _stateCurrent.Value = new StateHolder { State = value };
            }
        }
    }

    private sealed class StateHolder
    {
        public DbOperationState State;
    }
}