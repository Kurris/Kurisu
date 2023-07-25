using Grpc.Core;
using Kurisu.Grpc.Attributes;
using Kurisu.Test.Greet;
using Microsoft.AspNetCore.Authorization;

namespace Kurisu.Test.WebApi_A.GrpcServices;

[Authorize]
[GrpcImplement]
public class GreetService : Greeter.GreeterBase
{
    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        var reply = new HelloReply
        {
            Message = "你好" + request.Name
        };

        throw new Exception("测试错误");
    }
}