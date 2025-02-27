using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class SubTask
{
    public int SubTaskId { get; set; }

    public int TaskId { get; set; }

    public int AssignedToUserId { get; set; }

    public string SubTaskName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public virtual User AssignedToUser { get; set; } = null!;

    public virtual Task Task { get; set; } = null!;
}
