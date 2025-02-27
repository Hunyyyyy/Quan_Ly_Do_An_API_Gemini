using Quan_Ly_Do_An.Data;

namespace Quan_Ly_Do_An.Models
{
    public class ProjectTeamModel
    {
        public int TeamId { get; set; }
        public int ProjectId { get; set; }  // Liên kết với bảng Project
        public int UserId { get; set; }  // Liên kết với bảng User (Sinh viên)
        public string GroupNumber { get; set; }  // Số nhóm
        public string LeaderName { get; set; }  // Số nhóm
        public string ProjectName { get; set; }  // Số nhóm
        public bool IsLeader { get; set; }  // Đánh dấu sinh viên có phải là nhóm trưởng không
        public Project Project { get; set; }
        public User User { get; set; }
    }
}
