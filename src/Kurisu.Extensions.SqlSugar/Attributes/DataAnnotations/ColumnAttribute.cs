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

    private bool _isEnum;

    public bool IsEnum
    {
        get => _isEnum;
        set
        {
            _isEnum = value;
            if (value)
            {
                ColumnDataType = "varchar(30)";
                SqlParameterDbType = typeof(EnumToStringConvert);
            }
        }
    }

    private bool _isMoney;

    public bool IsMoney
    {
        get => _isMoney;
        set
        {
            _isMoney = value;
            if (value)
            {
                ColumnDataType = "decimal(18,2)";
            }
        }
    }

    private bool _isBoolean;

    public bool IsBoolean
    {
        get => _isBoolean;
        set
        {
            _isBoolean = value;
            if (value)
            {
                ColumnDataType = "bit";
            }
        }
    }
}