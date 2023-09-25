﻿namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations;

public class ShardingReadWriteContext
{
    [Obsolete("use DefaultEnableBehavior")]
    public bool DefaultReadEnable
    {
        get { return DefaultEnableBehavior == ReadWriteDefaultEnableBehavior.DefaultEnable; }
        set
        {
            DefaultEnableBehavior =
                value
                    ? ReadWriteDefaultEnableBehavior.DefaultEnable
                    : ReadWriteDefaultEnableBehavior.DefaultDisable;
        }
    }

    public ReadWriteDefaultEnableBehavior DefaultEnableBehavior { get; set; }
    public int DefaultPriority { get; set; }
    private readonly Dictionary<string /*数据源*/, string /*数据源对应的读节点名称*/> _dataSourceReadNode;

    private ShardingReadWriteContext()
    {
        DefaultReadEnable = false;
        DefaultEnableBehavior = ReadWriteDefaultEnableBehavior.DefaultDisable;
        DefaultPriority = 0;
        _dataSourceReadNode = new Dictionary<string, string>();
    }

    public static ShardingReadWriteContext Create()
    {
        return new ShardingReadWriteContext();
    }

    /// <summary>
    /// 添加数据源对应读节点获取名称
    /// </summary>
    /// <param name="dataSource"></param>
    /// <param name="readNodeName"></param>
    /// <returns></returns>
    public bool AddDataSourceReadNode(string dataSource, string readNodeName)
    {
        if (_dataSourceReadNode.ContainsKey(dataSource))
        {
            return false;
        }

        _dataSourceReadNode.Add(dataSource, readNodeName);
        return true;
    }

    /// <summary>
    /// 尝试获取对应数据源的读节点名称
    /// </summary>
    /// <param name="dataSource"></param>
    /// <param name="readNodeName"></param>
    /// <returns></returns>
    public bool TryGetDataSourceReadNode(string dataSource, out string readNodeName)
    {
        return _dataSourceReadNode.TryGetValue(dataSource, out readNodeName);
    }
}