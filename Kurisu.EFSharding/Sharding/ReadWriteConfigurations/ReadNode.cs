namespace Kurisu.EFSharding.Sharding.ReadWriteConfigurations;


public class ReadNode
{
    /// <summary>
    /// 当前读库节点名称
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// 当前读库链接的连接字符串
    /// </summary>
    public string ConnectionString { get; }

    public ReadNode(string name,string connectionString)
    {
        Name = name??throw new ArgumentNullException("read node name is null");
        ConnectionString = connectionString;
    }
}