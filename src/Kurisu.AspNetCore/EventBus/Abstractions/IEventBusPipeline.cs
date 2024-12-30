using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.EventBus.Abstractions;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public interface IEventBusPipeline<TRequest, TResponse> : IScopeDependency
    // where TRequest : IPipelinRequest<TResponse>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    Task<TResponse> InvokeAsync(TRequest request, InvokeDelegate<TResponse> next);
}
