using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class Project
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public int ClassId { get; set; }

    public int ProjectLeaderId { get; set; }

    public string? DevelopmentProcess { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? StartDate { get; set; }

    public virtual ICollection<Aidatum> Aidata { get; set; } = new List<Aidatum>();

    public virtual Class Class { get; set; } = null!;

    public virtual User ProjectLeader { get; set; } = null!;

    public virtual ICollection<ProjectMilestone> ProjectMilestones { get; set; } = new List<ProjectMilestone>();

    public virtual ICollection<ProjectTeam> ProjectTeams { get; set; } = new List<ProjectTeam>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
