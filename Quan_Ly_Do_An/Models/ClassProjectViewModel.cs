namespace Quan_Ly_Do_An.Models
{
    public class ClassProjectViewModel
    {
        public string ClassName { get; set; }
        public string ProjectName { get; set; }
        public string GroupNumber { get; set; }
        public string ProjectLeader { get; set; }
        public string SubjectCode { get; set; }
        public int ClassId { get; set; }
        public int ProjectId { get; set; }
    }
    public class ProgressReportAndClassProjectViewModel
    {
        public ClassProjectViewModel? ClassProject { get; set; }
        public List<string> ProgressReportDay { get; set; }
        public List<string> ProgressReportTime { get; set; }
        public List<ProgressReportModel> ProgressReport { get; set; }

    }
}
