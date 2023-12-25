using Kurisu.AspNetCore.Grpc.Abstractions;

namespace Kurisu.Test.WebApi_B.GrpcModules;

[Service(Lifetime =  ServiceLifetime.Singleton)]
public class AGrpcModule : IGrpcModule 
{
    public AGrpcModule()
    {
        
    }
    
    public string Service { get; set; } = "AService";
    public string DisplayName { get; set; } = "A web api 模块";
}