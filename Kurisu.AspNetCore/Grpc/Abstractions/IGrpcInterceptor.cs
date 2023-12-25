using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Grpc.Abstractions;

public interface IGrpcInterceptor : ISingletonDependency
{
}