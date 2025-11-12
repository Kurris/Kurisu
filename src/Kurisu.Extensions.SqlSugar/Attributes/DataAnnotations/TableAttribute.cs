using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Attributes.DataAnnotations;

public class TableAttribute : SugarTable
{
    public TableAttribute(string name, string comment) : base(name, comment)
    {
    }
}