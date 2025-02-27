namespace Quan_Ly_Do_An.Models
{
    public class UserModel
    {
        public int UserId { get; set; }

        public string Email { get; set; } = null!;

        public string Role { get; set; } = null!;

        public string? StudentCode { get; set; }

        public string FullName { get; set; } = null!;
    }
}
