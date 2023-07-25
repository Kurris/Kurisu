using System;
using Kurisu.Scope.Abstractions;
using Kurisu.Scope.Internal;

namespace Kurisu.Scope;

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