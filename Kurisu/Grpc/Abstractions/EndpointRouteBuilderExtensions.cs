using System;
using System.Linq;
using System.Reflection;
using Kurisu.Grpc.Attributes;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kurisu.Grpc.Abstractions
{
    /// <summary>
    /// 管道builder扩展类
    /// </summary>
    [SkipScan]
    public static class EndpointRouteBuilderExtensions
    {
        /// <summary>
        /// 映射Grpc服务
        /// </summary>
        /// <param name="endpoints"></param>
        public static void MapGrpcServices(this IEndpointRouteBuilder endpoints)
        {
            try
            {
                var assembly = Assembly.Load("Grpc.AspNetCore.Server");
                var type = assembly.GetTypes().FirstOrDefault(x => x.Name.Contains("GrpcEndpointRouteBuilderExtensions"));
                if (type != null)
                {
                    var method = type.GetMethod("MapGrpcService");

                    foreach (Type grpcImplement in App.ActiveTypes.Where(x => x.IsDefined(typeof(GrpcImplementAttribute))))
                    {
                        method.MakeGenericMethod(grpcImplement).Invoke(null, new object[] {endpoints});
                        App.Logger.LogInformation("发现并映射一个{Grpc}###{Name}", "Grpc服务", grpcImplement.FullName);
                    }
                }
                else
                {
                    App.Logger.LogWarning("映射Grpc服务失败:{Reason}", "找不到扩展方法Grpc.AspNetCore.Server.GrpcEndpointRouteBuilderExtensions");
                }
            }
            catch (Exception e)
            {
                App.Logger.LogInformation("映射Grpc服务失败:{Reason}", e.Message);
            }
        }
    }
}