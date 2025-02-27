namespace Quan_Ly_Do_An.Models
{
    public class ClassWithInstructorModel
    {
        public int ClassId { get; set; }

        public string ClassName { get; set; } = null!;

        public string SubjectCode { get; set; } = null!;

        public int InstructorId { get; set; }
        public string InstructorFullName { get; set; } = null!;
        public string InstructorEmail { get; set; } = null!;
        public List<ClassWithMembersModel> Members { get; set; }
        public List<ProjectWithTeamsModel> Projects { get; set; } = new List<ProjectWithTeamsModel>(); // Thêm Projects vào đây

    }
}
