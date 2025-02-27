namespace Quan_Ly_Do_An.Models
{
    public class ClassModel
    {
        public int ClassId { get; set; }

        public string ClassName { get; set; } = null!;

        public string SubjectCode { get; set; } = null!;
        public string InstructorFullName { get; set; } = null!;

        public int InstructorId { get; set; }
    }
}
