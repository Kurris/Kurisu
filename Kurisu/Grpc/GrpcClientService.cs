using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Kurisu.Authentication.Abstractions;
using Kurisu.Grpc.Abstractions;
using Microsoft.Extensions.Options;

namespace Kurisu.Grpc;

/// <summary>
/// Grpc客户端服务
/// </summary>
/// <typeparam name="TModule">Grpc模块</typeparam>
/// <typeparam name="TClient">Grpc客户端类型</typeparam>
public class GrpcClientService<TModule, TClient> : IGrpcClientService<TModule, TClient>
    where TModule : IGrpcModule
    where TClient : ClientBase
{
    private readonly ICurrentUserInfo _currentUserInfoResolver;
    private readonly Interceptor[] _grpcInterceptors;

    /// <summary>
    /// Grpc客户端
    /// </summary>
    private TClient _client;

    /// <summary>
    /// Grpc服务ip地址
    /// </summary>
    private readonly string _host;

    private readonly object _lock = new();

    private readonly bool _useTls;

    /// <summary>
    /// ctor singleton by IOC
    /// </summary>
    /// <param name="options"></param>
    /// <param name="currentUserInfoResolver"></param>
    /// <param name="grpcModule"></param>
    /// <param name="interceptors"></param>
    public GrpcClientService(IOptions<GrpcSetting> options, ICurrentUserInfo currentUserInfoResolver,
        TModule grpcModule, IEnumerable<IGrpcInterceptor> interceptors)
    {
        if (options.Value == null) throw new ArgumentNullException(nameof(options));
        _currentUserInfoResolver = currentUserInfoResolver;
        _grpcInterceptors = interceptors.Select(x => (Interceptor) x).ToArray();


        var grpcSetting = options.Value;
        _useTls = grpcSetting.UseTls;
        if (!grpcSetting.ServiceRoutes.TryGetValue(grpcModule.Service, out _host))
        {
            _host = "NotFound";
        }
    }

    public TClient Instance
    {
        get
        {
            //双if + lock
            if (_client == null)
            {
                lock (_lock)
                {
                    // new XXXClient(GrpcChannel.ForAddress("IP地址"));
                    var callInvoker = CreateAuthenticatedChannel(_host).Intercept(_grpcInterceptors);
                    _client ??= Activator.CreateInstance(typeof(TClient), callInvoker) as TClient;
                }
            }

            return _client;
        }
    }

    /// <summary>
    /// 创建授权管道
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    private GrpcChannel CreateAuthenticatedChannel(string address)
    {
        var credentials = CallCredentials.FromInterceptor(async (_, metadata) =>
        {
            var token = _currentUserInfoResolver.GetBearerToken();
            metadata.Add("Authorization", $"{token}");
            await Task.CompletedTask;
        });

        var options = new GrpcChannelOptions
        {
            //需要版本>=2.52.0才能在非tls中启用授权token
            //see:https://github.com/grpc/grpc-dotnet/pull/1802
            Credentials = ChannelCredentials.Create(_useTls ? ChannelCredentials.SecureSsl : ChannelCredentials.Insecure, credentials),
            UnsafeUseInsecureChannelCallCredentials = !_useTls,

            ServiceConfig = new ServiceConfig
            {
                MethodConfigs =
                {
                    new MethodConfig
                    {
                        Names =
                        {
                            MethodName.Default //应用在所有grpc方法
                        },
                        RetryPolicy = new RetryPolicy //重试
                        {
                            MaxAttempts = 5, //次数
                            InitialBackoff = TimeSpan.FromSeconds(1),
                            MaxBackoff = TimeSpan.FromSeconds(5),
                            BackoffMultiplier = 1.5,
                            RetryableStatusCodes =
                            {
                                StatusCode.Unavailable,
                                StatusCode.Internal
                            }
                        }
                    }
                }
            }
        };


        return GrpcChannel.ForAddress(address, options);
    }
}