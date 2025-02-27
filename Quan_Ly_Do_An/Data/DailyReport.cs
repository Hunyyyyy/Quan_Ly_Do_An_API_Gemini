using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class DailyReport
{
    public int ReportId { get; set; }

    public int TaskId { get; set; }

    public int UserId { get; set; }

    public DateTime? ReportDate { get; set; }

    public string? WorkDescription { get; set; }

    public int? TimeSpent { get; set; }

    public virtual Task Task { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
