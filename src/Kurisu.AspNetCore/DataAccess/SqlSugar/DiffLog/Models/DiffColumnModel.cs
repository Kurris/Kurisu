namespace Kurisu.AspNetCore.DataAccess.SqlSugar.DiffLog.Models;

internal class DiffColumnModel
{
    public string Column { get; set; }

    public string Description { get; set; }

    public string Value { get; set; }

    public bool IsPrimaryKey { get; set; }
}
