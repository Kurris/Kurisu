namespace Kurisu.SqlSugar.Services.Implements;

internal class SqlSugarOptionsService : ISqlSugarOptionsService
{
    public string Title { get; set; }

    public string RoutePath { get; set; }

    public bool Diff { get; set; }

    public Guid BatchNo { get; set; }

    public DateTime RaiseTime { get; set; }

    public bool IgnoreTenant { get; set; }
}