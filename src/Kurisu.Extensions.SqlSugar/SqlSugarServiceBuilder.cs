using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.ContextAccessor;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Context;
using Kurisu.Extensions.SqlSugar.Sharding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.SqlSugar;

/// <summary>
/// sqlsugar注入构建器
/// </summary>
public class SqlSugarServiceBuilder(IServiceCollection services)
{
    public bool UseSharding { get; set; }

    /// <summary>
    /// 使用自定义数据库审计用户访问器。
    /// </summary>
    /// <typeparam name="TAccessor"></typeparam>
    /// <returns></returns>
    public SqlSugarServiceBuilder UseAuditAccessor<TAccessor>() where TAccessor : class, IDbAuditAccessor
    {
        services.Replace(ServiceDescriptor.Singleton<IDbAuditAccessor, TAccessor>());
        return this;
    }

    /// <summary>
    /// 使用自定义数据库审计用户访问器。
    /// </summary>
    /// <param name="factory"></param>
    /// <returns></returns>
    public SqlSugarServiceBuilder UseAuditAccessor(Func<IServiceProvider, IDbAuditAccessor> factory)
    {
        services.Replace(ServiceDescriptor.Singleton(factory));
        return this;
    }

    /// <summary>
    /// 使用自定义数据库租户访问器。
    /// </summary>
    /// <typeparam name="TAccessor"></typeparam>
    /// <returns></returns>
    public SqlSugarServiceBuilder UseTenantAccessor<TAccessor>() where TAccessor : class, IDbTenantAccessor
    {
        services.Replace(ServiceDescriptor.Singleton<IDbTenantAccessor, TAccessor>());
        return this;
    }

    /// <summary>
    /// 使用自定义数据库租户访问器。
    /// </summary>
    /// <param name="factory"></param>
    /// <returns></returns>
    public SqlSugarServiceBuilder UseTenantAccessor(Func<IServiceProvider, IDbTenantAccessor> factory)
    {
        services.Replace(ServiceDescriptor.Singleton(factory));
        return this;
    }

    /// <summary>
    /// 使用自定义数据库审计时间访问器。
    /// </summary>
    /// <typeparam name="TClock"></typeparam>
    /// <returns></returns>
    public SqlSugarServiceBuilder UseClock<TClock>() where TClock : class, IDbClock
    {
        services.Replace(ServiceDescriptor.Singleton<IDbClock, TClock>());
        return this;
    }

    /// <summary>
    /// 使用自定义数据库审计时间访问器。
    /// </summary>
    /// <param name="factory"></param>
    /// <returns></returns>
    public SqlSugarServiceBuilder UseClock(Func<IServiceProvider, IDbClock> factory)
    {
        services.Replace(ServiceDescriptor.Singleton(factory));
        return this;
    }

    /// <summary>
    /// 使用 ICurrentUser 作为数据库上下文来源。
    /// </summary>
    /// <returns></returns>
    public SqlSugarServiceBuilder UseCurrentUserContext()
    {
        services.Replace(ServiceDescriptor.Singleton<IDbAuditAccessor, CurrentUserDbAuditAccessor>());
        services.Replace(ServiceDescriptor.Singleton<IDbTenantAccessor, CurrentUserDbTenantAccessor>());
        return this;
    }

    /// <summary>
    /// 启用shading功能
    /// </summary>
    /// <returns></returns>
    public SqlSugarServiceBuilder EnableSharding()
    {
        services.Replace(ServiceDescriptor.Describe(typeof(IDbContext), typeof(ShardingContext), ServiceLifetime.Scoped));

        services.Replace(ServiceDescriptor.Describe(typeof(IContextAccessor<DbOperationState>), typeof(ShardingStateAccessor), ServiceLifetime.Singleton));

        this.UseSharding = true;
        return this;
    }
}

internal class ShardingStateAccessor(ILogger<ShardingStateAccessor> logger) : AbstractContextAccessor<DbOperationState>(logger)
{
    private static readonly AsyncLocal<StateHolder> _stateCurrent = new();

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
