namespace Quan_Ly_Do_An.Models
{
    public class GroupProgressViewModel
    {
        public string ProjectName { get; set; }
        public int CompletionPercentage1 { get; set; }
        public int TotalTasksCreated { get; set; } // Tổng số nhiệm vụ đã tạo
        public int TotalTasksAssigned { get; set; } // Tổng số nhiệm vụ đã nhận
        public int TotalSubTasksCreated { get; set; } // Tổng số công việc phụ đã tạo
        public int TotalReportsSubmitted { get; set; } // Tổng số báo cáo tiến độ đã nộp
        public int TotalReportsCompleted { get; set; } // Tổng số báo cáo hoàn thiện
        public int TotalReportsFail { get; set; } // Tổng số báo cáo bị từ chối

        // Số nhiệm vụ hoàn thành dựa trên báo cáo hoàn thiện
        public int TotalTasksCompleted { get; set; }

        // Tính tỷ lệ hoàn thành
        public int CompletionPercentage => TotalTasksCreated == 0
            ? 0
            : (int)((double)TotalTasksCompleted / TotalTasksCreated * 100);

        // Tính tỷ lệ báo cáo hoàn thiện (không bắt buộc)
        public int ReportCompletionPercentage => TotalReportsSubmitted == 0
            ? 0
            : (int)((double)TotalReportsCompleted / TotalReportsSubmitted * 100);
        public string CompletionRate {  get; set; }
        public string TaskCompletionRate {  get; set; }

    }
    public class UserProgressViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int ReportsSubmitted { get; set; }
        public int ReportsCompleted { get; set; }
        public int ReportsRejected { get; set; }
        public int ReportsInProgress { get; set; }
        public int TotalTasksAssigned { get; set; }
        public double CompletionRate { get; set; }
    }

}
