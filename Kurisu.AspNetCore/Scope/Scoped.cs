using System;
using Kurisu.AspNetCore.Scope.Abstractions;
using Kurisu.AspNetCore.Scope.Internal;

namespace Kurisu.AspNetCore.Scope;

/// <summary>
/// 局部作用域
/// </summary>
public static class Scoped
{
    /// <summary>
    /// 临时作用域,使用完立即释放
    /// </summary>
    public static Lazy<IScope> Temp => new(() => new TempScope());

    /// <summary>
    /// 请求作用域,在当前请求后释放
    /// </summary>
    public static Lazy<IScope> Request => new(() => new RequestScope());
}