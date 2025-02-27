namespace Quan_Ly_Do_An.Models
{
    public class ClassProjectProgressViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public List<ProjectProgressDetail> Projects { get; set; }
    }
    public class ClassDetailViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public List<ClassProjectViewModel> Class { get; set; } // Dự án trong lớp
        public List<ProjectProgressDetail> GroupProgressDetails { get; set; } // Tiến độ nhóm
        public GroupCompletionStatsViewModel GroupCompletionStats { get; set; } // Thống kê hoàn thành
        public ReportSubmissionStatsViewModel ReportSubmissionStats { get; set; } // Báo cáo định kỳ
        public DashboardStatisticsViewModel ProjectStatistics { get; set; } // Báo cáo định kỳ
        public List<GroupProgressViewModel>GroupProgressView { get; set; } // Báo cáo định kỳ
    }



    public class ProjectProgressDetail
    {
        public string ProjectName { get; set; }
        public List<GroupProgressDetail> Groups { get; set; }
    }

    public class GroupProgressDetail
    {
        public string GroupNumber { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
        public double ProgressPercentage { get; set; }
    }
    public class GroupCompletionStatsViewModel
    {
        public int CompletedGroups { get; set; }
        public int IncompleteGroups { get; set; }
    }
    public class ReportSubmissionStatsViewModel
    {
        public int TotalReports { get; set; }
        public int OnTimeReports { get; set; }
        public int LateReports { get; set; }
    }

}
