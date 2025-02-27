namespace Quan_Ly_Do_An.Models
{
    public class ClassWithDetailsModel
    {
        public ClassWithInstructorModel Instructor { get; set; }
        public List<ClassWithMembersModel> Members { get; set; }
    }
}
