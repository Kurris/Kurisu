using Kurisu.AspNetCore.Abstractions.Startup;

namespace Kurisu.Extensions.ContextAccessor.Abstractions;


/// <summary>
/// 上下文访问器
/// </summary>
/// <typeparam name="TContext"></typeparam>
public interface IContextAccessor<out TContext> : IAppAsyncLocalLifecycle where TContext : class, new()
{
    TContext Current { get; }
}
