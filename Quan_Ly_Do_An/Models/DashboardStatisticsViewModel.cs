namespace Quan_Ly_Do_An.Models
{
    public class DashboardStatisticsViewModel
    {
        public int TotalProjects { get; set; }
        public int TotalStudents { get; set; }
        public double AverageCompletionRate { get; set; }
        public int TotalReportsApproved { get; set; }
        public int TotalReportsRejected { get; set; }
        public List<InstructorClassViewModel> InstructorClassView { get; set; }
        public List<ReportSubmissionStatsViewModel2> ReportSubmissionStatsView { get; set; }
       
        public List<ClassProgressViewModel> ClassProgressView { get; set; }
        public List<ProjectWithCompletion> ProjectsWithCompletion { get; set; }

        public List<ProjectStatisticsDetailViewModel> ProjectDetails { get; set; } = new List<ProjectStatisticsDetailViewModel>();
    }

    public class ProjectStatisticsDetailViewModel
    {
        public string ProjectName { get; set; }
        public int TotalTasksCreated { get; set; }
        public int TotalReportsSubmitted { get; set; }
        public int TotalReportsCompleted { get; set; }
        public int CompletionRate { get; set; } // Tỷ lệ hoàn thành (phần trăm)
    }
    public class InstructorClassViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string SubjectCode { get; set; }
        public int TotalStudents { get; set; }
        public int TotalProjects { get; set; }
    }

    public class ClassProgressViewModel
    {
        public string ClassName { get; set; }
        public int CompletedGroups { get; set; }
        public int TotalGroups { get; set; }
        public double ProgressPercent { get; set; }
    }
    public class ReportSubmissionStatsViewModel2
    {
        public string ClassName { get; set; }
        public double OnTimePercent { get; set; }
        public double LatePercent { get; set; }
        public double ApprovedPercent { get; set; }
        public double RejectedPercent { get; set; }
        public int TotalReports { get; set; }
        public int OnTimeReports { get; set; }
        public int ApprovedReports { get; set; }
        public int RejectedReports { get; set; }
    }


    public class ImportantNotificationViewModel
    {
        public string Notification { get; set; }
        public string ClassName { get; set; }
    }

    public class ProjectWithCompletion
    {
        public Data.Project Project { get; set; }
        public double CompletionPercentage { get; set; }
        public int RemainingDays { get; set; }
        public int TotalDay { get; set; }

    }

}
