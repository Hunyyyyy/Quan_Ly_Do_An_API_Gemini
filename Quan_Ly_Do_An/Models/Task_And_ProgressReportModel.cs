namespace Quan_Ly_Do_An.Models
{
    public class Task_And_ProgressReportModel
    {
        public TaskModel? Task { get; set; }
        public List<ProgressReportModel>? Report { get; set; }
        public List<SubTaskModel>? SubTask { get; set; }
        public List<ProjectMilestoneModel>? Phases { get; set; }

    }
}
