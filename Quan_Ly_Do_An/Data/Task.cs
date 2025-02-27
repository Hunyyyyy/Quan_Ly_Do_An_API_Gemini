using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class Task
{
    public int TaskId { get; set; }

    public int ProjectId { get; set; }

    public int? AssignedToUserId { get; set; }

    public string TaskName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public string? SubmittedFileTypes { get; set; }

    public string? Attachment { get; set; }

    public string? AdditionalNotes { get; set; }

    public string? PriorityLevel { get; set; }

    public virtual User? AssignedToUser { get; set; }

    public virtual ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();

    public virtual Project Project { get; set; } = null!;

    public virtual ICollection<SubTask> SubTasks { get; set; } = new List<SubTask>();
}
