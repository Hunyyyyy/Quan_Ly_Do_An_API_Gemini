using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Quan_Ly_Do_An.Data;
using Quan_Ly_Do_An.Models;

namespace Quan_Ly_Do_An.Controllers
{
    public class WorkSpaceController : Controller
    {
        private readonly DatabaseDoAnContext _dbAnContext;
        private readonly IHttpContextAccessor _contextAccessor;
        public WorkSpaceController(IHttpContextAccessor contextAccessor, DatabaseDoAnContext dbAnContext)
        {
            _contextAccessor = contextAccessor;
            _dbAnContext = dbAnContext;
        }
        public async Task<IActionResult> Index()
        {
            var email = _contextAccessor.HttpContext.Session.GetString("Email");

            // Lấy tất cả lớp học từ bảng Class
            var classes = await _dbAnContext.Classes
                .Select(c => new ClassModel
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    SubjectCode = c.SubjectCode,
                    InstructorId = c.InstructorId // Giả sử bảng Class có InstructorId
                })
                .ToListAsync();

            // Kiểm tra nếu có lớp học
            if (classes.Any())
            {
                // Lấy thông tin người dùng từ bảng User
                var giangVien = await _dbAnContext.Users
                    .Where(u => u.Email == email)
                    .Select(u => new UserModel
                    {
                        Email = u.Email,
                        FullName = u.FullName,
                        Role = u.Role,
                        StudentCode = u.StudentCode,
                        UserId = u.UserId
                    })
                    .FirstOrDefaultAsync();

                // Kiểm tra nếu giảng viên tồn tại và lấy danh sách các lớp học có InstructorId là giảng viên này
                if (giangVien != null)
                {
                    var classWithInstructors = classes
                        .Where(c => c.InstructorId == giangVien.UserId) // Lọc lớp học theo giảng viên
                        .Select(c => new ClassWithInstructorModel
                        {
                            ClassId = c.ClassId,
                            ClassName = c.ClassName,
                            SubjectCode = c.SubjectCode,
                            InstructorFullName = giangVien.FullName, // Thêm tên giảng viên vào thông tin lớp học
                            InstructorEmail = giangVien.Email
                        })
                        .ToList();

                    return View(classWithInstructors); // Trả về View với danh sách lớp học của giảng viên
                }
                else
                {
                    // Nếu không tìm thấy giảng viên, bạn có thể xử lý như trả về một trang lỗi hoặc thông báo
                    ModelState.AddModelError("", "Không tìm thấy giảng viên.");
                    return View("Index","Account"); // Trả về trang mặc định
                }
            }
            else
            {
                // Nếu không có lớp học
                
                return View(); // Trả về trang mặc định
            }
        }

        /*      public async Task<IActionResult> GetClassTableData(string email)
              {

              }*/
        [HttpPost]
        public async Task<IActionResult> CreateWorkSpace(IFormFile? fileClassList, string nameSpace, string subjectCode)
        {
            // Kiểm tra dữ liệu đầu vào
            if (fileClassList == null || string.IsNullOrWhiteSpace(nameSpace) || string.IsNullOrWhiteSpace(subjectCode))
            {
                TempData["Message"] = "Vui lòng nhập đầy đủ thông tin!";
                return View("Index");
            }

            var email = _contextAccessor.HttpContext.Session.GetString("Email");
            var user = await _dbAnContext.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Kiểm tra xem giảng viên có tồn tại không
            var instructorId = user?.UserId;
            if (instructorId == null)
            {
                TempData["Message"] = "Vui lòng nhập đầy đủ thông tin!";
                return View("Index");
            }
                        // Tạo đối tượng Class mới nhưng chưa lưu vào CSDL
            var newClass = new Data.Class
            {
                ClassName = nameSpace,
                SubjectCode = subjectCode.ToUpper(),
                InstructorId = (int)instructorId
            };

            // Kiểm tra và xử lý file Excel
            try
            {
                if (fileClassList.Length > 0)
                {
                    using (var stream = new MemoryStream())
                    {
                        await fileClassList.CopyToAsync(stream);
                        using (var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets[0];
                            int rowCount = worksheet.Dimension.Rows;

                            // Lấy danh sách tất cả mã sinh viên từ bảng User
                            var allStudentCodes = await _dbAnContext.Users
                                         .Where(u => u.Role == "Student")
                                         .Select(u => new { u.StudentCode, u.UserId })
                                         .ToListAsync(); // Truy vấn dữ liệu về bộ nhớ dưới dạng danh sách

                            // Sử dụng Trim() để loại bỏ khoảng trắng thừa từ mã sinh viên
                            var studentCodeDictionary = allStudentCodes
                                .Where(u => !string.IsNullOrWhiteSpace(u.StudentCode)) // Loại bỏ các sinh viên có StudentCode trống hoặc chỉ có khoảng trắng
                                .ToDictionary(u => u.StudentCode.Trim(), u => u.UserId);

                            var classMembers = new List<ClassMember>();

                            for (int row = 2; row <= rowCount; row++)
                            {
                                var studentCode = worksheet.Cells[row, 2].Text.Trim();


                                if (studentCodeDictionary.TryGetValue(studentCode.Trim(), out var userId))
                                {
                                    var classMember = new ClassMember
                                    {
                                        ClassId = newClass.ClassId,
                                        UserId = userId,
                                        StudentCode = studentCode.Trim(),
                                    };
                                    classMembers.Add(classMember);
                                }
                            };

                            if (classMembers.Count > 0)
                            {
                                _dbAnContext.Classes.Add(newClass);
                                await _dbAnContext.SaveChangesAsync();

                                classMembers.ForEach(cm => cm.ClassId = newClass.ClassId);
                                await _dbAnContext.ClassMembers.AddRangeAsync(classMembers);
                                await _dbAnContext.SaveChangesAsync();
                                TempData["Message"] = $"Bạn vừa tạo lớp {nameSpace} mã môn {subjectCode} thành công!";
                                return RedirectToAction("Index");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json("Error: " + ex.Message);
            }
            TempData["Message"] = "Tạo lớp mới thất bại,vui lòng thử lại";
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> UpdateWorkSpace(string nameSpace, string subjectCode,int classId)
        {
            var classToUpdate = await _dbAnContext.Classes
                       .SingleOrDefaultAsync(c => c.ClassId == classId);

            if (classToUpdate != null)
            {
                // Cập nhật NameSpace mới
                classToUpdate.ClassName = nameSpace;
                classToUpdate.SubjectCode = subjectCode;

                // Lưu các thay đổi
                await _dbAnContext.SaveChangesAsync();
            }
            TempData["Message"] = "Cập nhật thành công!";
            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteWorkSpace(int classId, string nameSpace, string subjectCode)
        {
            try
            {
                var classToDelete = await _dbAnContext.Classes
                            .SingleOrDefaultAsync(c => c.ClassId == classId);

                if (classToDelete != null)
                {
                    // Lấy các thành viên của lớp có ClassId phù hợp
                    var classMemberToDelete = await _dbAnContext.ClassMembers
                            .Where(c => c.ClassId == classId)  // Lọc theo ClassId
                            .ToListAsync();  // Chuyển đổi thành danh sách
                    var projectToDelete = await _dbAnContext.Projects
                        .Where(c => c.ClassId == classId)
                        .ToListAsync();
                    var projectTeamToDelete = await _dbAnContext.ProjectTeams
                        .Where(c => c.ClassId == classId)
                        .ToListAsync();


                    // Tiến hành xóa hoặc xử lý với các bản ghi đã lấy
                    _dbAnContext.ProjectTeams.RemoveRange(projectTeamToDelete);
                    _dbAnContext.ClassMembers.RemoveRange(classMemberToDelete);
                    _dbAnContext.Projects.RemoveRange(projectToDelete);
                    _dbAnContext.Classes.Remove(classToDelete);

                    // Lưu thay đổi vào cơ sở dữ liệu
                    await _dbAnContext.SaveChangesAsync();
                    //<strong class="me-auto">Thông báo</strong>
                    TempData["Message"] = $"Bạn vừa xóa thành công! lớp {nameSpace} mã môn {subjectCode}";
                }
                else
                {
                    TempData["Message"] = "Không tìm thấy lớp cần xóa!";
                }

                // Chuyển hướng về Index sau khi xóa
                return RedirectToAction("Index");
            }catch(Exception ex)
            {
                TempData["Message"] = "Lớp đang hoạt động,không thể xóa!";
                return RedirectToAction("Index");
            }
          
            
        }

    }
}
