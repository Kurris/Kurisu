﻿namespace Kurisu.SqlSugar.DiffLog.Models;

internal class DiffTableModel
{
    public string Table { get; set; }

    public List<DiffColumnModel> Columns { get; set; }
}
