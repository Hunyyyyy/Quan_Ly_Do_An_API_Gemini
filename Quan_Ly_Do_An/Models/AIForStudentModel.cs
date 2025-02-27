namespace Quan_Ly_Do_An.Models
{
    public class AIForStudentModel
    {
        public List<TaskModel> Tasks { get; set; }
        public List<TaskModel>TasksRecived { get; set; }
        public List<ProgressReportModel> ProgressReport { get; set; }
        public List<ProgressReportModel> TasksFinalReport { get; set; }
        public ProjectProgressViewModel ProjectProgressView { get; set; }
        public ProjectMilestoneAndProjectMemberView_GroupModel ProjectMilestoneAndProjectMemberView_Group { get; set; }
        public List<ProjectPhaseViewModel> ProjectPhaseView { get; set; }

    }
}
