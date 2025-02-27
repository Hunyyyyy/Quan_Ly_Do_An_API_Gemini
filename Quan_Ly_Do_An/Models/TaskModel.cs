using Microsoft.AspNetCore.Mvc;

namespace Quan_Ly_Do_An.Models
{
    public class TaskModel
    {
            public int TaskId { get; set; }
            public int ProjectId { get; set; }
            public int? AssignedToUserId { get; set; } = null;
            public string? TaskName { get; set; }
            public string? ProjectName { get; set; }
            public string? AssignedUserName { get; set; }
            public string? Description { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string Status { get; set; } = "Đang chờ"; // Trạng thái mặc định là "Đang chờ"
            [BindProperty]
            public List<string>? SubmissionTypes { get; set; }
            public IFormFile? Attachments { get; set; }
             public string? AttachmentPath { get; set; }
            public string? Notes { get; set; }

            public string? Priority { get; set; }

    }
}
