using Kurisu.AspNetCore.Abstractions.DataAccess;

namespace Kurisu.Test.DataAccess.Filter.Mock;

/// <summary>
/// 可配置的数据权限提供者 Mock，测试用
/// </summary>
public class MockDataPermissionProvider : IGetDataPermissions
{
    private readonly Dictionary<Type, Dictionary<string, IReadOnlyList<object>>> _permissions = new();

    /// <summary>
    /// 为指定实体类型设置数据权限过滤规则
    /// </summary>
    /// <param name="propertyName">属性名</param>
    /// <param name="values">允许的值列表</param>
    public void SetPermission<T>(string propertyName, IReadOnlyList<object> values)
    {
        _permissions[typeof(T)] = new Dictionary<string, IReadOnlyList<object>>
        {
            [propertyName] = values
        };
    }

    /// <inheritdoc />
    public Dictionary<string, IReadOnlyList<object>> GetData<T>()
    {
        return _permissions.TryGetValue(typeof(T), out var data)
            ? data
            : new Dictionary<string, IReadOnlyList<object>>();
    }
}
