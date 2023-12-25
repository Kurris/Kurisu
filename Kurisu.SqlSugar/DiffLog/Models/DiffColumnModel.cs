namespace Kurisu.SqlSugar.DiffLog.Models;

internal class DiffColumnModel
{
    public string Column { get; set; }

    public string Descriptoin { get; set; }

    public string Value { get; set; }

    public bool IsPrimaryKey { get; set; }
}
