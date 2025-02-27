namespace Quan_Ly_Do_An.Models
{
    public class ProjectWithTeamsModel
    {
        public int GroupNumber { get; set; }
        public int UserId { get; set; }
        public bool IsLeader { get; set; }
        public string ProjectName { get; set; }
        public int Score { get; set; }
        public string Note { get; set; }
        public int ProjectId { get; set; }
        public int ProjectLeaderId { get; set; }
        public UserModel ProjectLeader { get; set; }
        public List<ProjectTeamModel> Teams { get; set; }
        public List<ProjectModel> Project { get; set; }
    }
}
