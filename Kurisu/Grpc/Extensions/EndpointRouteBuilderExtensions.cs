using System;
using System.Linq;
using System.Reflection;
using Kurisu;
using Kurisu.Grpc.Attributes;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 终结点builder扩展类
/// </summary>
[SkipScan]
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// 映射Grpc服务 Nuget Grpc.AspNetCore
    /// </summary>
    /// <param name="endpoints"></param>
    public static void MapGrpcServices(this IEndpointRouteBuilder endpoints)
    {
        try
        {
            var assembly = Assembly.Load("Grpc.AspNetCore.Server");
            var extensions = assembly.GetTypes().FirstOrDefault(x => x.Name.Contains("GrpcEndpointRouteBuilderExtensions"));
            if (extensions != null)
            {
                var method = extensions.GetMethod("MapGrpcService");
                if (method != null)
                {
                    foreach (var grpcImplement in App.ActiveTypes.Where(x => x.IsDefined(typeof(GrpcImplementAttribute))))
                    {
                        method.MakeGenericMethod(grpcImplement).Invoke(null, new object[] {endpoints});
                        App.Logger.LogInformation("发现并映射Grpc服务###{Name}", grpcImplement.FullName);
                    }
                }
            }
            else
            {
                App.Logger.LogWarning("映射Grpc服务失败:{Reason}", "找不到扩展方法Grpc.AspNetCore.Server.GrpcEndpointRouteBuilderExtensions");
            }
        }
        catch (Exception e)
        {
            App.Logger.LogError("Grpc Services Mapping Error:{Reason}", e.Message);
        }
    }
}