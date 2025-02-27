using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class ClassMember
{
    public int ClassMemberId { get; set; }

    public int ClassId { get; set; }

    public int UserId { get; set; }

    public string? StudentCode { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
