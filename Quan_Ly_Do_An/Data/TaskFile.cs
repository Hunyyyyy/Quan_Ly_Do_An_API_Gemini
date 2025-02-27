using System;
using System.Collections.Generic;

namespace Quan_Ly_Do_An.Data;

public partial class TaskFile
{
    public int FileId { get; set; }

    public int TaskId { get; set; }

    public string FileName { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public DateTime? UploadDate { get; set; }

    public virtual Task Task { get; set; } = null!;
}
