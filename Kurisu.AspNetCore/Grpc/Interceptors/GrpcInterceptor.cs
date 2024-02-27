//using System.Threading.Tasks;
//using Grpc.Core;
//using Grpc.Core.Interceptors;
//using Kurisu.AspNetCore.Grpc.Abstractions;
//using Kurisu.AspNetCore.UnifyResultAndValidation;
//using Microsoft.Extensions.Logging;

//namespace Kurisu.AspNetCore.Grpc.Interceptors;

//public class GrpcInterceptor : Interceptor, IGrpcInterceptor
//{
//    private readonly ILogger<GrpcInterceptor> _logger;

//    public GrpcInterceptor(ILogger<GrpcInterceptor> logger)
//    {
//        _logger = logger;
//    }

//    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
//    {
//        _logger.LogInformation("Starting call. Method:{Method}", context.Method.Name);

//        var call = continuation(request, context);

//        return new AsyncUnaryCall<TResponse>(
//            HandleResponse(call.ResponseAsync),
//            call.ResponseHeadersAsync,
//            call.GetStatus,
//            call.GetTrailers,
//            call.Dispose);
//    }

//    private async Task<TResponse> HandleResponse<TResponse>(Task<TResponse> inner)
//    {
//        try
//        {
//            return await inner;
//        }
//        catch (RpcException ex)
//        {
//            var message = ex.Status.Detail.Replace("Exception was thrown by handler. Exception: ", "");
//            _logger.LogError("End call. Error:{Error}", message);
//            throw new UserFriendlyException(message, ex);
//        }
//    }
//}