using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor;

/// <summary>
/// 数据访问builder
/// </summary>
internal class KurisuDataAccessorBuilder : IKurisuDataAccessorBuilder
{
    /// <summary>
    /// 服务容器
    /// </summary>
    public IServiceCollection Services { get; set; }

    /// <summary>
    /// 配置处理
    /// </summary>
    public List<Action<KurisuDataAccessorSettingBuilder>> ConfigurationBuilders { get; set; }
}