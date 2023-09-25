using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccess.Functions.Default.Abstractions;

/// <summary>
/// 模型配置来源处理器
/// </summary>
public interface IModelConfigurationSourceResolver : ISingletonDependency
{
    /// <summary>
    /// 获取配置类所在的程序集
    /// </summary>
    /// <returns></returns>
    Assembly GetAssembly();
}