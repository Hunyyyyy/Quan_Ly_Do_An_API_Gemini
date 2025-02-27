namespace Quan_Ly_Do_An.Models
{
    public class ProgressReportModel
    {
        public int ProgressReportId { get; set; }

        public int TaskId { get; set; }

        public int? UserId { get; set; }
        public int ReportId { get; set; }

        public DateTime? ReportDate { get; set; }

        public string? WorkDescription { get; set; }

        public IFormFile? AttachedFile { get; set; }

        public string? AttachedFilePath { get; set; }

        public string? Status { get; set; } 

        public string? ReminderStatus { get; set; } = "Chưa nhắc nhở";
        public string? ReporterName { get; set; }
        public string ClassName { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectLeaderName { get; set; }
        public string? InstructorName { get; set; }
        public string? TaskName { get;set; }
        public string? UserName { get;set; }
        public string? Email { get;set; }

    }
}
