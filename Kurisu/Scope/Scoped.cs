using System;
using Kurisu.Scope.Abstractions;
using Kurisu.Scope.Internal;

namespace Kurisu.Scope
{
    /// <summary>
    /// 局部作用域
    /// </summary>
    public sealed class Scoped
    {
        /// <summary>
        /// 临时作用域,使用完立即释放
        /// </summary>
        public static Lazy<IScope> Template { get; } = new(() => new TemplateScope());

        /// <summary>
        /// 请求作用域,在当前请求后释放
        /// </summary>
        public static Lazy<IScope> Request { get; } = new(() => new RequestScope());
    }
}