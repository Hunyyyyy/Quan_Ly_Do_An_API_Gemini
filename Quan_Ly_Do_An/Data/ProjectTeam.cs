using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class ProjectTeam
{
    public int TeamId { get; set; }

    public int ProjectId { get; set; }

    public int UserId { get; set; }

    public string? GroupNumber { get; set; }

    public int ClassId { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
