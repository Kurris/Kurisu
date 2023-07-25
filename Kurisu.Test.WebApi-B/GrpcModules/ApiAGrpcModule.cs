using Kurisu.Grpc.Abstractions;

namespace Kurisu.Test.WebApi_B.GrpcModules;

public class ApiAGrpcModule : IGrpcModule
{
    public string Service { get; set; } = "AService";
    public string DisplayName { get; set; } = "A web api 模块";
}