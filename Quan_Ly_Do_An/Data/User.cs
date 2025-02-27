using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? StudentCode { get; set; }

    public string FullName { get; set; } = null!;

    public virtual ICollection<Aidatum> Aidata { get; set; } = new List<Aidatum>();

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Notification> NotificationReceivers { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    public virtual ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();

    public virtual ICollection<ProjectTeam> ProjectTeams { get; set; } = new List<ProjectTeam>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<SubTask> SubTasks { get; set; } = new List<SubTask>();

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
