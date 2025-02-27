namespace Quan_Ly_Do_An.Models
{
    public class StudentProgressViewModel
    {
        public string StudentName { get; set; }
        public int TotalTasksAssigned { get; set; }
        public int TotalTasksCompleted { get; set; }
        public int TotalReportsSubmitted { get; set; }
        public int TotalReportsFailed { get; set; }
        public int CompletionPercentage { get; set; }
    }
}
