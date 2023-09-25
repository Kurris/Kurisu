namespace Kurisu.EFSharding;

public class DatasourceUnit
{
    public DatasourceUnit()
    {
    }

    public DatasourceUnit(bool isDefault, string name, string connectionString)
    {
        IsDefault = isDefault;
        Name = name;
        ConnectionString = connectionString;
    }

    public bool IsDefault { get; set; }
    public string Name { get; set; }
    public string ConnectionString { get; set; }
}