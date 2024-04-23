using System.Threading.Tasks;

namespace Kurisu.AspNetCore.EventBus;

public delegate Task<TResponse> InvokeDelegate<TResponse>();
