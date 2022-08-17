using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Kurisu.Authentication.Abstractions;
using Kurisu.Grpc.Abstractions;
using Microsoft.Extensions.Options;

namespace Kurisu.Grpc.Internal
{
    /// <summary>
    /// Grpc客户端服务
    /// </summary>
    /// <typeparam name="TGrpcModule">Grpc远程模块</typeparam>
    /// <typeparam name="TClient">Grpc客户端类型</typeparam>
    public class GrpcClientService<TGrpcModule, TClient> : IGrpcClientService<TGrpcModule, TClient> where TClient : ClientBase where TGrpcModule : IGrpcModule
    {
        private TClient _client;

        private readonly TGrpcModule _grpcModule;
        private readonly GrpcSetting _grpcSetting;

        public GrpcClientService(IOptions<GrpcSetting> options, TGrpcModule grpcModule)
        {
            _grpcModule = grpcModule;
            _grpcSetting = options.Value;
        }

        private readonly object _lock = new();

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
                    // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
                    if (_client == null)
                    {
                        _client = Activator.CreateInstance(typeof(TClient), CreateAuthenticatedChannel(GetHost())) as TClient;
                    }
                }
            }

            return _client;
        }

        /// <summary>
        /// 获取配置host
        /// </summary>
        /// <returns></returns>
        private string GetHost()
        {
            return _grpcSetting.ServiceRoutes.TryGetValue(_grpcModule.Key, out var value) ? value : string.Empty;
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
                // UnsafeUseInsecureChannelCallCredentials = true,
                // Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, credentials),
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