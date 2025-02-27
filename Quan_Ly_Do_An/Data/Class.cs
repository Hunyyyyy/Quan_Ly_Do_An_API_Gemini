using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class Class
{
    public int ClassId { get; set; }

    public string ClassName { get; set; } = null!;

    public string SubjectCode { get; set; } = null!;

    public int InstructorId { get; set; }

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual User Instructor { get; set; } = null!;

    public virtual ICollection<ProjectTeam> ProjectTeams { get; set; } = new List<ProjectTeam>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
