using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Kurisu.Grpc;

public class ServiceLoggerInterceptor : Interceptor
{
    private readonly ILogger<ServiceLoggerInterceptor> _logger;

    public ServiceLoggerInterceptor(ILogger<ServiceLoggerInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        _logger.LogInformation("Starting receiving call. Type/Method: {Type} / {Method}",
            MethodType.Unary, context.Method);
        try
        {
            return await continuation(request, context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error thrown by {Method}.{Message}", context.Method, ex.Message);
            throw;
        }
    }
}