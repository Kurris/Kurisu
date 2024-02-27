//using Grpc.Core;
//using Microsoft.Extensions.DependencyInjection;

//namespace Kurisu.AspNetCore.Grpc.Abstractions;

///// <summary>
///// Grpc客户端服务
///// </summary>
///// <typeparam name="TModule">Grpc模块</typeparam>
///// <typeparam name="TClient">Grpc客户端类型</typeparam>
///// <remarks>
///// Grpc.Net.Client;Google.Protobuf;Grpc.Tools
///// </remarks>
//// ReSharper disable once UnusedTypeParameter
//// ReSharper disable once TypeParameterCanBeVariant
//public interface IGrpcClientService<TModule, TClient> : ISingletonDependency
//    where TModule : IGrpcModule
//    where TClient : ClientBase
//{
//    /// <summary>
//    /// Grpc客户端
//    /// </summary>
//    TClient Instance { get; }
//}