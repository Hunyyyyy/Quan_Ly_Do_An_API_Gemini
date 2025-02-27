using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class ProgressReport
{
    public int ProgressReportId { get; set; }

    public int TaskId { get; set; }

    public int UserId { get; set; }

    public DateTime? ReportDate { get; set; }

    public string? WorkDescription { get; set; }

    public string? AttachedFile { get; set; }

    public string? Status { get; set; }

    public string? ReminderStatus { get; set; }

    public virtual Task Task { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
