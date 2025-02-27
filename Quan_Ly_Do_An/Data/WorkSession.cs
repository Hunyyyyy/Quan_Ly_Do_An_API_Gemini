using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class WorkSession
{
    public int SessionId { get; set; }

    public int TaskId { get; set; }

    public int UserId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public bool? IsSessionActive { get; set; }

    public virtual Task Task { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
