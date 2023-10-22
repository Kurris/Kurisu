
using System;

namespace Kurisu.Authentication.Abstractions;

/// <summary>
/// 当前用户信息处理器
/// </summary>
public interface ICurrentUserInfo
{
    /// <summary>
    /// 获取用户id
    /// </summary>
    /// <returns></returns>
    T GetSubjectId<T>();

    /// <summary>
    /// uid类型subject id
    /// </summary>
    /// <returns></returns>
    Guid GetUidSubjectId();

    /// <summary>
    /// string类型subject id
    /// </summary>
    /// <returns></returns>
    string GetStringSubjectId();

    /// <summary>
    /// int类型subject id
    /// </summary>
    /// <returns></returns>
    int GetIntSubjectId();

    /// <summary>
    /// 获取用户请求access_token
    /// </summary>
    /// <returns></returns>
    string GetBearerToken();
}