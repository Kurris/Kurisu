#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AspectCore.Extensions.DependencyInjection;
using AspectCore.Extensions.Hosting;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.DistributedLock;
using Kurisu.AspNetCore.Abstractions.DistributedLock.Aop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

/// <summary>验证官方代理与宿主、事务和作用域切面的集成, 不连接外部基础设施.</summary>
public class AspectCoreIntegrationTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<CallState>();
        services.AddScoped<IDbContext>(provider => StubProxy.Create<IDbContext>((method, args) =>
        {
            var state = provider.GetRequiredService<CallState>();
            return method.Name switch
            {
                "get_DatasourceManager" => StubProxy.Create<IDatasourceManager>((member, _) =>
                    member.Name == "CreateTransScope" ? new TransactionScope(state) : throw new NotSupportedException()),
                "UseTenant" => state.Enter("tenant:" + args![0]),
                "EnableDataPermission" => state.Enter("permission"),
                _ => throw new NotSupportedException(method.Name)
            };
        }));
        services.AddScoped<ILockable, TestLockable>();
        services.AddScoped<IApplication, Application>();
        services.AddKeyedScoped<IApplication, Application>("named");
        return services.BuildDynamicProxyProvider();
    }

    [Fact]
    public void Hosting_PreservesMicrosoftServiceProvider()
    {
        using var host = Host.CreateDefaultBuilder().UseDynamicProxy().Build();
        Assert.IsType<ServiceProvider>(host.Services);
    }

    [Fact]
    public async Task BusinessException_RollsBackTransaction()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IApplication>().FailAsync());
        Assert.Equal(new[] { "begin", "body", "rollback", "transaction-dispose" },
            scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    [Fact]
    public async Task TenantAndPermissionScopes_AreReleasedAfterFailure()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredService<IApplication>().TenantAsync("tenant-1"));
        var events = scope.ServiceProvider.GetRequiredService<CallState>().Events;
        Assert.Contains("tenant:tenant-1-enter", events);
        Assert.Contains("permission-enter", events);
        Assert.Contains("tenant:tenant-1-exit", events);
        Assert.Contains("permission-exit", events);
        Assert.Equal("body", events[2]);
    }

    [Fact]
    public async Task KeyedService_AcquiresAndReleasesLockAfterFailure()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.ServiceProvider.GetRequiredKeyedService<IApplication>("named").LockAsync("42"));
        Assert.Equal(new[] { "lock:Locker:test:42", "body", "unlock" },
            scope.ServiceProvider.GetRequiredService<CallState>().Events);
    }

    public interface IApplication
    {
        Task FailAsync();
        Task TenantAsync(string tenantId);
        Task LockAsync(string id);
    }

    public class Application(CallState state) : IApplication
    {
        [Transactional]
        public virtual Task FailAsync() => Fail();
        [UseTenant]
        [EnableDataPermission]
        public virtual Task TenantAsync(string tenantId) => Fail();
        [TryLock("test", "操作失败")]
        public virtual Task LockAsync(string id) => Fail();

        private Task Fail()
        {
            state.Events.Add("body");
            return Task.FromException(new InvalidOperationException("业务失败"));
        }
    }

    public class CallState
    {
        public List<string> Events { get; } = [];
        public IDisposable Enter(string name)
        {
            Events.Add(name + "-enter");
            return new Cleanup(() => Events.Add(name + "-exit"));
        }
    }

    private sealed class Cleanup(Action action) : IDisposable { public void Dispose() => action(); }
    private sealed class TransactionScope(CallState state) : ITransactionScope
    {
        public Task BeginAsync() { state.Events.Add("begin"); return Task.CompletedTask; }
        public Task CommitAsync() { state.Events.Add("commit"); return Task.CompletedTask; }
        public Task RollbackAsync() { state.Events.Add("rollback"); return Task.CompletedTask; }
        public void Dispose() => state.Events.Add("transaction-dispose");
    }

    public class TestLockable(CallState state) : ILockable
    {
        public Task<ILockHandler> LockAsync(string key, DistributedLockAcquisitionOptions options,
            CancellationToken cancellationToken = default)
        {
            state.Events.Add("lock:" + key);
            return Task.FromResult<ILockHandler>(new LockHandler(state));
        }
    }

    private sealed class LockHandler(CallState state) : ILockHandler
    {
        public bool Acquired => true;
        public ValueTask DisposeAsync() { state.Events.Add("unlock"); return ValueTask.CompletedTask; }
    }

    public class StubProxy : DispatchProxy
    {
        private Func<MethodInfo, object?[]?, object?> _handler = null!;
        public static T Create<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
        {
            var proxy = DispatchProxy.Create<T, StubProxy>();
            ((StubProxy)(object)proxy)._handler = handler;
            return proxy;
        }
        protected override object? Invoke(MethodInfo? method, object?[]? args) => _handler(method!, args);
    }
}
