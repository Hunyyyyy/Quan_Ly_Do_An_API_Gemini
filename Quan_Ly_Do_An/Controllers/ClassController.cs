using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_Ly_Do_An.Data;
using Quan_Ly_Do_An.Models;

namespace Quan_Ly_Do_An.Controllers
{
    public class ClassController : Controller
    {

        private readonly DatabaseDoAnContext _dbAnContext;
        private readonly IHttpContextAccessor _contextAccessor;
        public ClassController(IHttpContextAccessor contextAccessor, DatabaseDoAnContext dbAnContext)
        {
            _contextAccessor = contextAccessor;
            _dbAnContext = dbAnContext;
        }
        public async Task<IActionResult> Index(int classId)
        {
            try
            {
                // Gọi phương thức lấy dữ liệu lớp và giảng viên
                var classWithDetails = await GetClassDetailsAsync(classId);
                // Kiểm tra xem lớp đã có dữ liệu (dự án, nhóm trưởng, nhóm chưa?)
                bool hasData = await _dbAnContext.Projects
                    .AnyAsync(p => p.ClassId == classId);  // Kiểm tra xem có bất kỳ dự án nào thuộc lớp này hay chưa

                // Truyền thông tin này vào View
                ViewBag.HasData = hasData;
                // Truyền dữ liệu vào View
                ViewBag.ClassId = classId;
                TempData["ClassId"] = classId;
                return View(classWithDetails);  // Trả về View với dữ liệu lớp học và giảng viên
            }
            catch (Exception ex)
            {
                // Nếu có lỗi xảy ra (ví dụ không tìm thấy giảng viên hoặc lớp học), trả về thông báo lỗi
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");  // Hoặc chuyển hướng tới trang đăng nhập
            }// Trả về View với dữ liệu lớp học và giảng viên
        }

        public async Task<ClassWithDetailsModel> GetClassDetailsAsync(int classId)
        {
            // Lấy email từ Session
            var email = _contextAccessor.HttpContext.Session.GetString("Email");
            if (email == null)
            {
                throw new Exception("User is not logged in.");
            }

            // Truy vấn giảng viên theo email
            var instructor = await _dbAnContext.Users
                .Where(u => u.Email == email)
                .Select(u => new { u.UserId, u.FullName })
                .FirstOrDefaultAsync();

            if (instructor == null)
            {
                throw new Exception("Instructor not found.");
            }

            var query = from c in _dbAnContext.Classes
                        join cm in _dbAnContext.ClassMembers on c.ClassId equals cm.ClassId
                        join u in _dbAnContext.Users on cm.UserId equals u.UserId
                        join pt in _dbAnContext.ProjectTeams
                            on new { cm.UserId, c.ClassId } equals new { pt.UserId, pt.ClassId } into projectTeamJoin
                        from pt in projectTeamJoin.DefaultIfEmpty() // Left join với ProjectTeams theo ClassId và UserId
                        join p in _dbAnContext.Projects
                            on new { pt.ProjectId, c.ClassId } equals new { p.ProjectId, p.ClassId } into projectJoin
                        from p in projectJoin.DefaultIfEmpty() // Left join với Projects theo ClassId và ProjectId
                        join i in _dbAnContext.Users on c.InstructorId equals i.UserId
                        where c.ClassId == classId // Chỉ lấy dữ liệu của lớp cụ thể
                        orderby c.ClassId, pt.GroupNumber
                        select new
                        {
                            UserId = u.UserId,
                            StudentFullName = u.FullName,
                            StudentCode = u.StudentCode,
                            StudentEmail = u.Email,
                            GroupNumber = pt != null && p != null && pt.ProjectId == p.ProjectId ? pt.GroupNumber : null,
                            IsLeader = pt != null && pt.UserId == p.ProjectLeaderId ? "Yes" : "No",
                            ProjectName = p.ProjectName,
                            ClassName = c.ClassName,
                            SubjectCode = c.SubjectCode,
                            InstructorFullName = i.FullName
                        };

            var result = await query.AsNoTracking().ToListAsync();

            // Chuyển đổi kết quả thành mô hình ClassWithMembersModel
            var members = result.Select(r => new ClassWithMembersModel
            {
                StudentCode = r.StudentCode,
                ClassName = r.ClassName,
                GroupNumber = r.GroupNumber,
                InstructorFullName = r.InstructorFullName,
                StudentEmail = r.StudentEmail,
                StudentFullName = r.StudentFullName,
                SubjectCode = r.SubjectCode,
                IsLeader = r.IsLeader,
                ProjectName = r.ProjectName,
                UserId = r.UserId,
            }).ToList();

            // Tạo đối tượng ClassWithInstructorModel
            var instructorModel = new ClassWithInstructorModel
            {
                ClassId = classId,
                InstructorId = instructor.UserId,
                InstructorFullName = instructor.FullName
            };

            // Tạo đối tượng ClassWithDetailsModel
            var classWithDetails = new ClassWithDetailsModel
            {
                Instructor = instructorModel,
                Members = members
            };

            return classWithDetails;
        }




        [HttpPost]
        public async Task<IActionResult> SaveAllProjects(List<ProjectModel> projects, int classId)
        {
            var classWithDetails = await GetClassDetailsAsync(classId);

            // Kiểm tra nếu classId không có trong TempData
            if (TempData["ClassId"] == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin lớp học!";
                return View("Index",classWithDetails);
            }

            int tempClassId = Convert.ToInt32(TempData["ClassId"]);

            // Kiểm tra nếu projects không null và có dữ liệu
            if (projects != null && projects.Any())
            {
                // Nhóm các thành viên theo GroupNumber
                var groups = projects.GroupBy(p => p.GroupNumber);

                // Kiểm tra và xác định nhóm trưởng của từng nhóm
                foreach (var group in groups)
                {
                    var leaders = group.Where(p => p.IsLeader).ToList();

                    // Kiểm tra số lượng nhóm trưởng
                    if (leaders.Count != 1)
                    {
                        TempData["Message"] = $"Nhóm {group.Key} phải có đúng 1 nhóm trưởng.";
                        // Truyền dữ liệu vào View
                        TempData["ClassId"] = classId;
                        return View("Index", classWithDetails); // Quay lại trang Index nếu có lỗi
                    }

                    // Lấy người được chọn làm nhóm trưởng
                    var projectLeader = leaders.First();

                    // Tạo một Project mới cho nhóm này
                    var project = new Project
                    {
                        ProjectName = projectLeader.ProjectName,
                        ClassId = classId, // Chỉ định lớp học cụ thể
                        ProjectLeaderId = projectLeader.UserId
                    };

                    // Thêm vào bảng Projects bằng LINQ
                    _dbAnContext.Projects.Add(project);
                    await _dbAnContext.SaveChangesAsync();

                    // Tạo các thành viên trong nhóm cho ProjectTeam
                    foreach (var member in group)
                    {
                        var projectTeam = new ProjectTeam
                        {
                            ProjectId = project.ProjectId, // Liên kết với Project
                            UserId = member.UserId, // Liên kết với người dùng (sinh viên)
                            GroupNumber = member.GroupNumber.ToString(), // Số nhóm
                            ClassId = classId // Lưu ClassId vào ProjectTeams
                        };

                        // Thêm thành viên vào bảng ProjectTeams bằng LINQ
                        _dbAnContext.ProjectTeams.Add(projectTeam);
                    }
                }

                // Lưu tất cả thay đổi vào cơ sở dữ liệu
                await _dbAnContext.SaveChangesAsync();

                TempData["Message"] = "Lưu dữ liệu thành công!";
                TempData["ClassId"] = classId;
                return View("Index", classWithDetails);
            }

            // Nếu không có dữ liệu, trả về thông báo lỗi
            TempData["Message"] = "Lưu dữ liệu thất bại, vui lòng thử lại sau!";
            TempData["ClassId"] = classId;
            return View("Index", classWithDetails);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProjects(List<ProjectModel> projects, int classId)
        {
            var classWithDetails = await GetClassDetailsAsync(classId);

            // Kiểm tra nếu classId không có trong TempData
            if (TempData["ClassId"] == null)
            {
                TempData["Message"] = "Không tìm thấy thông tin lớp học!";
                return View("Index", classWithDetails);
            }

            int tempClassId = Convert.ToInt32(TempData["ClassId"]);

            // Kiểm tra nếu projects không null và có dữ liệu
            if (projects != null && projects.Any())
            {
                // Nhóm các thành viên theo GroupNumber
                var groups = projects.GroupBy(p => p.GroupNumber);

                // Kiểm tra và xác định nhóm trưởng của từng nhóm
                foreach (var group in groups)
                {
                    var leaders = group.Where(p => p.IsLeader).ToList();

                    // Kiểm tra số lượng nhóm trưởng
                    if (leaders.Count != 1)
                    {
                        TempData["Message"] = $"Nhóm {group.Key} phải có đúng 1 nhóm trưởng.";
                        // Truyền dữ liệu vào View
                        TempData["ClassId"] = classId;
                        return View("Index", classWithDetails); // Quay lại trang Index nếu có lỗi
                    }

                    // Lấy người được chọn làm nhóm trưởng
                    var projectLeader = leaders.First();

                    // Cập nhật thông tin dự án (nếu đã tồn tại)
                    var existingProject = await _dbAnContext.Projects
                        .FirstOrDefaultAsync(p => p.ClassId == classId && p.ProjectLeaderId == projectLeader.UserId);

                    if (existingProject != null)
                    {
                        // Cập nhật tên đồ án nếu có thay đổi
                        if (existingProject.ProjectName != projectLeader.ProjectName)
                        {
                            existingProject.ProjectName = projectLeader.ProjectName;
                            _dbAnContext.Projects.Update(existingProject); // Chỉ cập nhật nếu có thay đổi
                        }

                        // Cập nhật nhóm trưởng nếu có thay đổi (có thể cần kiểm tra ID nhóm trưởng mới)
                        if (existingProject.ProjectLeaderId != projectLeader.UserId)
                        {
                            existingProject.ProjectLeaderId = projectLeader.UserId;
                            _dbAnContext.Projects.Update(existingProject); // Cập nhật lại nhóm trưởng
                        }
                    }
                    else
                    {
                        // Nếu không tồn tại, thông báo lỗi hoặc xử lý theo yêu cầu
                        TempData["Message"] = $"Dự án cho nhóm trưởng {projectLeader.ProjectLeaderId} không tồn tại.";
                        return View("Index", classWithDetails);
                    }

                    // Cập nhật lại thông tin các thành viên trong nhóm
                    foreach (var member in group)
                    {
                        // Kiểm tra nếu thành viên này đã có trong bảng ProjectTeams
                        var existingProjectTeam = await _dbAnContext.ProjectTeams
                            .FirstOrDefaultAsync(pt => pt.UserId == member.UserId && pt.ProjectId == existingProject.ProjectId);

                        if (existingProjectTeam != null)
                        {
                            // Kiểm tra xem GroupNumber có thay đổi không
                            if (existingProjectTeam.GroupNumber != member.GroupNumber.ToString())
                            {
                                existingProjectTeam.GroupNumber = member.GroupNumber.ToString(); // Chỉ cập nhật nếu có thay đổi
                                _dbAnContext.ProjectTeams.Update(existingProjectTeam);
                            }
                        }
                        else
                        {
                            // Nếu chưa có trong bảng ProjectTeams, thêm mới
                            var newProjectTeam = new ProjectTeam
                            {
                                ProjectId = existingProject.ProjectId,
                                UserId = member.UserId,
                                GroupNumber = member.GroupNumber.ToString(),
                                ClassId = classId
                            };
                            _dbAnContext.ProjectTeams.Add(newProjectTeam);
                        }
                    }
                }

                // Lưu tất cả thay đổi vào cơ sở dữ liệu
                await _dbAnContext.SaveChangesAsync();

                TempData["Message"] = "Cập nhật dữ liệu thành công!";
                TempData["ClassId"] = classId;
                return View("Index", classWithDetails);
            }

            // Nếu không có dữ liệu, trả về thông báo lỗi
            TempData["Message"] = "Cập nhật dữ liệu thất bại, vui lòng thử lại sau!";
            TempData["ClassId"] = classId;
            return View("Index", classWithDetails);
        }


    }
}
