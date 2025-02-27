using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class Aidatum
{
    public int AidataId { get; set; }

    public int ProjectId { get; set; }

    public int UserId { get; set; }

    public string Question { get; set; } = null!;

    public string Airesponse { get; set; } = null!;

    public string? ProcessStage { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
