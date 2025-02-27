namespace Quan_Ly_Do_An.Models
{
    public class ProjectMemberView_GroupModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int ClassId { get; set; }
        public int ProjectLeaderId { get; set; }
        public int MemberUserId { get; set; }
        public string GroupNumber { get; set; }
        public string RoleInProject { get; set; }
    }
}
