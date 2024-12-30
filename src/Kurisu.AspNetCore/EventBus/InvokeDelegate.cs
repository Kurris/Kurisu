using System.Threading.Tasks;

namespace Kurisu.AspNetCore.EventBus;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TResponse"></typeparam>
public delegate Task<TResponse> InvokeDelegate<TResponse>();
