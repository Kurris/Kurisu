namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

/// <summary>
/// 数据库查询过滤器操作接口，提供租户、软删除、数据权限等过滤条件的运行时控制能力。
/// </summary>
public interface IFilterOperator
{
    /// <summary>
    /// 忽略当前数据库操作的租户过滤，返回 <see cref="IDisposable"/> 用于恢复。
    /// <para>注意：<see cref="IgnoreTenant"/> 优先级高于 <see cref="UseTenant"/>，
    /// 在 IgnoreTenant 作用域内调用 UseTenant 不会重新添加租户过滤。</para>
    /// </summary>
    /// <returns>释放时恢复租户过滤的作用域对象。</returns>
    IDisposable IgnoreTenant();

    /// <summary>
    /// 指定当前数据库操作使用的租户ID。
    /// <para>自动替换查询过滤器中的 <see cref="Contract.Field.ITenantId.TenantId"/> 条件，
    /// 并在插入时自动填充实体上的 <see cref="Contract.Field.ITenantId.TenantId"/> 字段。</para>
    /// <para>支持嵌套作用域，内层释放后自动恢复外层租户。</para>
    /// </summary>
    /// <param name="tenantId">租户ID，不能为 null 或空白。</param>
    /// <returns>释放时恢复原租户过滤的作用域对象。</returns>
    IDisposable UseTenant(string tenantId);

    /// <summary>
    /// 忽略当前数据库操作的逻辑删除过滤（<see cref="Contract.Field.ISoftDeleted"/>），返回 <see cref="IDisposable"/> 用于恢复。
    /// </summary>
    /// <returns>释放时恢复逻辑删除过滤的作用域对象。</returns>
    IDisposable IgnoreSoftDeleted();

    /// <summary>
    /// 启用当前数据库操作的数据权限过滤。
    /// </summary>
    /// <returns>释放时恢复的作用域对象。</returns>
    IDisposable EnableDataPermission();

    /// <summary>
    /// 启用跨租户查询，等价于忽略租户过滤并标记跨租户状态。
    /// <para>调用此方法后，查询将不再自动添加租户条件，允许跨租户访问数据。</para>
    /// </summary>
    /// <returns>释放时恢复租户过滤的作用域对象。</returns>
    IDisposable EnableCrossTenant();

    /// <summary>
    /// 忽略当前数据库操作的分表路由，查询仅操作基础表。
    /// </summary>
    /// <returns>释放时恢复分表路由的作用域对象。</returns>
    IDisposable IgnoreSharding();
}
