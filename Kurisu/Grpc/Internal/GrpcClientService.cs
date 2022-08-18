using System;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Kurisu.Grpc.Abstractions;
using Microsoft.Extensions.Options;

namespace Kurisu.Grpc.Internal
{
    /// <summary>
    /// Grpc客户端服务
    /// </summary>
    /// <typeparam name="TGrpcModule">Grpc远程模块</typeparam>
    /// <typeparam name="TClient">Grpc客户端类型</typeparam>
    public class GrpcClientService<TGrpcModule, TClient> : IGrpcClientService<TGrpcModule, TClient>
        where TClient : ClientBase
        where TGrpcModule : IGrpcModule
    {
        private TClient _client;

        private readonly string _host;

        private readonly object _lock = new();

        /// <summary>
        /// ctor singleton by IOC
        /// </summary>
        /// <param name="options"></param>
        /// <param name="grpcModule"></param>
        public GrpcClientService(IOptions<GrpcSetting> options, TGrpcModule grpcModule)
        {
            var grpcSetting = options.Value;

            if (!grpcSetting.ServiceRoutes.TryGetValue(grpcModule.Key, out _host))
            {
                _host = "unknow";
            }
        }


        /// <summary>
        /// 创建Grpc客户端
        /// </summary>
        /// <returns></returns>
        public TClient Create()
        {
            if (_client == null)
            {
                lock (_lock)
                {
                    _client ??= Activator.CreateInstance(typeof(TClient), CreateAuthenticatedChannel(_host)) as TClient;
                }
            }

            return _client;
        }

        /// <summary>
        /// 创建授权管道
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private GrpcChannel CreateAuthenticatedChannel(string address)
        {
            return GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                ServiceConfig = new ServiceConfig
                {
                    MethodConfigs =
                    {
                        new MethodConfig
                        {
                            Names = {MethodName.Default},
                            RetryPolicy = new RetryPolicy
                            {
                                MaxAttempts = 5,
                                InitialBackoff = TimeSpan.FromSeconds(1),
                                MaxBackoff = TimeSpan.FromSeconds(5),
                                BackoffMultiplier = 1.5,
                                RetryableStatusCodes = {StatusCode.Unavailable, StatusCode.Internal}
                            }
                        }
                    }
                }
            });
        }
    }
}