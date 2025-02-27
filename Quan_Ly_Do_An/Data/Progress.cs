using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class Progress
{
    public int ProgressId { get; set; }

    public int ProjectId { get; set; }

    public int? TaskId { get; set; }

    public int? CompletedPercentage { get; set; }

    public DateTime? UpdateDate { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual Task? Task { get; set; }
}
