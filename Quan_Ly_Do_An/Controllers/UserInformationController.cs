using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_Ly_Do_An.Data;
using Quan_Ly_Do_An.Models;

namespace Quan_Ly_Do_An.Controllers
{
    public class UserInformationController : Controller
    {
        private readonly DatabaseDoAnContext _dbAnContext;
        private readonly IHttpContextAccessor _contextAccessor;
        public UserInformationController(IHttpContextAccessor contextAccessor, DatabaseDoAnContext dbAnContext)
        {
            _contextAccessor = contextAccessor;
            _dbAnContext = dbAnContext;
        }
        public async Task<IActionResult> Index()
        {
            var email = _contextAccessor.HttpContext.Session.GetString("Email");
            if (email == null) { 
            return RedirectToAction("Index","Account");
            }
            var user = await _dbAnContext.Users
                .Where(u => u.Email == email)
                .Select(u => new UserModel
                {
                    Email = u.Email,
                    Role = u.Role,
                    StudentCode = u.StudentCode,
                    FullName = u.FullName
                })
                .FirstOrDefaultAsync();
          
            return View(user);
        }
        [HttpPost]
        public IActionResult UpdateUserTable(string stdentcode,string fullname)
        {
            var email = _contextAccessor.HttpContext.Session.GetString("Email");

            var user = _dbAnContext.Users.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                user.StudentCode = stdentcode;
                user.FullName = fullname;
                _dbAnContext.SaveChanges();
                TempData["SuccessMessage"] = "Đã cập nhật lại thong tin cá nhân thành công !";
                // Trả về view (ví dụ như "Index" của controller "Home" hoặc một view khác)
                return RedirectToAction("Index", "UserInformation");
            }
            else
            {
                TempData["ErrorMessage"] = "Cập nhật thất bại,vui lòng thử lại sau!";
                return RedirectToAction("Index", "UserInformation");
            }

            // Nếu không tìm thấy user hoặc cập nhật thất bại, có thể trả về một view lỗi
    
            return Json(new { success = "Đã có lỗi xảy ra vui lòng thử lại sau" });// Hoặc một view khác nếu cần
        }
    }
}
