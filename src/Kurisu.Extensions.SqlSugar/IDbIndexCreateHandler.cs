namespace Kurisu.Extensions.SqlSugar;

public interface IIndexConfigurator
{
    List<IndexModel> GetIndexConfigs();
}

public class IndexModel
{
    public string IndexName { get; set; }

    public string[] ColumnNames { get; set; }

    public bool IsUnique { get; set; }
}