using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor;

/// <summary>
/// 数据库访问builder
/// </summary>
[SkipScan]
public interface IKurisuDataAccessorBuilder
{
    /// <summary>
    /// 服务容器
    /// </summary>
    IServiceCollection Services { get; set; }

    /// <summary>
    /// 配置处理
    /// </summary>
    List<Action<KurisuDataAccessorSettingBuilder>> ConfigurationBuilders { get; set; }
}