using SqlSugar;
using SqlSugar.DbConvert;

namespace Kurisu.Extensions.SqlSugar.Attributes.DataAnnotations;

public class ColumnAttribute : SugarColumn
{
    public ColumnAttribute(string comment, bool isNullable)
    {
        ColumnDescription = comment;
        IsNullable = isNullable;
    }

    public bool IsEnum
    {
        get => true;
        set
        {
            if (!value) return;
            ColumnDataType = "varchar(30)";
            SqlParameterDbType = typeof(EnumToStringConvert);
        }
    }

    public bool IsMoney
    {
        get => true;
        set
        {
            if (!value) return;
            ColumnDataType = "decimal(18,2)";
        }
    }

    public bool IsBoolean
    {
        get => true;
        set
        {
            if (!value) return;
            ColumnDataType = "bit";
        }
    }
}