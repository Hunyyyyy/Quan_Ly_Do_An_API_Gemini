using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class ProjectMilestone
{
    public int MilestoneId { get; set; }

    public int ProjectId { get; set; }

    public string StageName { get; set; } = null!;

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateOnly FinalDay { get; set; }

    public virtual Project Project { get; set; } = null!;
}
