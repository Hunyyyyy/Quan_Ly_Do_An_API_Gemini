using Microsoft.AspNetCore.Mvc;

namespace Quan_Ly_Do_An.Models
{
    public class ProjectModel
    {
        public int ProjectId { get; set; }
        public int GroupNumber { get; set; }
        public int UserId { get; set; }
        public int ClassId { get; set; }
        public int ProjectLeaderId { get; set; }
        public bool IsLeader { get; set; }
        public string ProjectName { get; set; }
        public int Score { get; set; }
        public string Note { get; set; }
    }
    public class MemberTaskProgressViewModel
    {
        public string MemberName { get; set; } // Tên thành viên
        public int AssignedTo { get; set; }   // ID người được giao
        public int TotalTasks { get; set; }   // Tổng số nhiệm vụ
        public int TotalSubTasks { get; set; } // Tổng số công việc phụ
        public int CompletedSubTasks { get; set; } // Số công việc phụ hoàn thành
        public int ProgressPercentage { get; set; } 
    }
    public class ProjectPhaseComparisonViewModel
    {
        public int MilestoneId { get; set; }
        public string StageName { get; set; }
        public int PlannedDays { get; set; }
        public int ActualProgress { get; set; }
        public string Status { get; set; }
        public int DaysElapsed { get; set; }
        public int TotalSubTasks { get; set; }
        public int CompletedSubTasks { get; set; }
    }
    public class ProjectProgressViewModel
    {
        public List<MemberTaskProgressViewModel> MemberProgress { get; set; }
        public List<ProjectPhaseComparisonViewModel> PhaseComparison { get; set; }
        public List<UserProgressViewModel> UserProgressView { get; set; }
        public ProjectIssuesViewModel ProjectIssuesView { get; set; }  
        public GroupProgressViewModel GroupProgress { get; set; }
        public List<ProjectPhaseViewModel>  GetProjectPhases { get; set; }
        public List<ProgressReportModel> ProgressReportView { get; set; }
    }
    public class CombinedViewModel
    {
        public ProjectProgressViewModel ProjectProgress { get; set; }
        public ProgressReportAndClassProjectViewModel ProgressReportAndClassProject { get; set; }
    }


    public class ProjectIssuesViewModel
    {
        public List<TaskIssueViewModel> TasksWithDeadlineIssues { get; set; }
        public List<MemberIssueViewModel> MembersNotUpdatingProgress { get; set; }
        public List<MemberIssueViewModel> MembersWithoutTasks { get; set; }
    }
    public class TaskIssueViewModel
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }
        public string UpdatedStatus { get; set; }
        public int DaysRemaining { get; set; }

    }
    public class MemberIssueViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }



}
