using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime? DateSent { get; set; }

    public bool? IsRead { get; set; }

    public int ReceiverId { get; set; }

    public virtual User Receiver { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
