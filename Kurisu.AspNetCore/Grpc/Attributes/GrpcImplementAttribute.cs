using System;

namespace Kurisu.AspNetCore.Grpc.Attributes;

/// <summary>
/// 标记为Grpc实现类
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GrpcImplementAttribute : Attribute
{
}