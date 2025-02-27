using Quan_Ly_Do_An.Data;

namespace Quan_Ly_Do_An.Models
{
    public class SubTaskModel
    {
        public int SubTaskId { get; set; }

        public int TaskId { get; set; }

        public int? AssignedToUserId { get; set; }

        public string SubTaskName { get; set; } = null!;
        public string TaskName { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? Status { get; set; }

        public UserModel AssignedToUser { get; set; } = null!;

        public List< TaskModel >Task { get; set; } = null!;
    }
}
