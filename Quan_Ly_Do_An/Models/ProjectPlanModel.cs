using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Quan_Ly_Do_An.Models
{
    public class ProjectPlanModel
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<Phase> Phases { get; set; }

    }
    public class Phase
    {

        public string StageName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime FinalDay { get; set; }
    }
    public class ProjectPhaseViewModel
    {

        public string StageName { get; set; }
        public int TotalDays { get; set; }
        public int Percentage { get; set; } // Phần trăm độ dài giai đoạn
        public string BackgroundColor { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
    public class ProjectMilestoneModel
    {

        public int MilestoneId { get; set; }

        public int ProjectId { get; set; }

        public string StageName { get; set; } = null!;

        public string? Description { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly EndDate { get; set; }

        public DateOnly FinalDay { get; set; }
        
    }
    public class ProjectMilestoneAndProjectMemberView_GroupModel
    {
        public ProjectMemberView_GroupModel projectInfo { get; set; }
        public List<ProjectMilestoneModel> projectMilestone{ get; set; }
    }

}
