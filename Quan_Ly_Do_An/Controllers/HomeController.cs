using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Quan_Ly_Do_An.Class;
using Quan_Ly_Do_An.Data;
using Quan_Ly_Do_An.Models;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace Quan_Ly_Do_An.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly DatabaseDoAnContext _dbAnContext;

        private int? userId;
        private int? classId;
        private int? projectId;
        private string? email;
        private string? userName;
        public HomeController(ILogger<HomeController> logger, IHttpContextAccessor contextAccessor,DatabaseDoAnContext dbAnContext)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _dbAnContext = dbAnContext;
           
            userId = _contextAccessor.HttpContext.Session.GetInt32("UserId");
            email = _contextAccessor.HttpContext.Session.GetString("Email");
            classId = _contextAccessor.HttpContext.Session.GetInt32("classId");
            projectId = _contextAccessor.HttpContext.Session.GetInt32("ProjectId");
            userName = _contextAccessor.HttpContext.Session.GetString("UserName");
        }

        public async Task<IActionResult> Index()
        {
            var email = _contextAccessor.HttpContext.Session.GetString("Email");
            var Isteacher = await _dbAnContext.Users.SingleOrDefaultAsync(x => x.UserId == userId);
            if (Isteacher.Role.Equals("Instructor"))
            {
                var instructorDashboard = await InstructorDashboard();
                var getClassProgress = await GetClassProgress((int)userId);
                var projectsWithCompletion = await GetInstructorProjectsWithCompletion((int)userId);
                _contextAccessor.HttpContext.Session.SetString("IsInstructor", "Instructor");
                var dashBoard = new DashboardStatisticsViewModel
                {
                    InstructorClassView = instructorDashboard,
                    // ReportSubmissionStatsView = getReportSubmissionStats,
                    ClassProgressView = getClassProgress,
                    ProjectsWithCompletion=projectsWithCompletion

                };
                return View(dashBoard);
            }
          return View();
        }
        public async Task<List<InstructorClassViewModel>> InstructorDashboard()
        {
            // Lấy danh sách lớp học mà giảng viên đang phụ trách
            var instructorClasses = await _dbAnContext.Classes
                .Where(c => c.InstructorId == userId) // Giả sử có thuộc tính TeacherId trong bảng Classes
                .Select(c => new
                {
                    c.ClassId,
                    c.ClassName,
                    c.SubjectCode,
                    TotalStudents = _dbAnContext.ClassMembers.Count(cm => cm.ClassId == c.ClassId),
                    TotalProjects = _dbAnContext.Projects.Count(p => p.ClassId == c.ClassId)
                })
                .ToListAsync();

            var classViewModels = instructorClasses.Select(c => new InstructorClassViewModel
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName,
                SubjectCode = c.SubjectCode,
                TotalStudents = c.TotalStudents,
                TotalProjects = c.TotalProjects
            }).ToList();

            return classViewModels; // Trả về View với danh sách các lớp học
        }
        public async Task<List<ClassProgressViewModel>> GetClassProgress(int instructorId)
        {
            var classProgress = await _dbAnContext.Classes
                .Where(c => c.InstructorId == instructorId)
                .Select(c => new
                {
                    ClassName = c.ClassName,
                    TotalGroups = _dbAnContext.ProjectTeams
                        .Where(pt => pt.ClassId == c.ClassId)
                        .GroupBy(pt => pt.GroupNumber)
                        .Count(),
                    CompletedGroups = _dbAnContext.ProjectTeams
                        .Where(pt => pt.ClassId == c.ClassId)
                        .GroupBy(pt => pt.GroupNumber)
                        .Count(g => g.All(pt => _dbAnContext.Tasks
                            .Where(t => t.ProjectId == pt.ProjectId)
                            .Any() && // Kiểm tra xem nhóm có ít nhất 1 nhiệm vụ không
                            g.All(pt => _dbAnContext.Tasks
                                .Where(t => t.ProjectId == pt.ProjectId)
                                .All(t => t.Status == "Hoàn thành"))))
                })
                .ToListAsync();

            var progressModels = classProgress.Select(cp => new ClassProgressViewModel
            {
                ClassName = cp.ClassName,
                CompletedGroups = cp.CompletedGroups,
                TotalGroups = cp.TotalGroups,
                ProgressPercent = cp.TotalGroups == 0 ? 0 : (double)cp.CompletedGroups / cp.TotalGroups * 100
            }).ToList();

            return progressModels; // Trả về View với Model là danh sách tiến độ
        }
      
        // Hàm tính tiến độ hoàn thành của dự án
        public async Task<double> CalculateProjectCompletionAndSendReminder(int projectId)
        {
            var project = await _dbAnContext.Projects.FindAsync(projectId);
            if (project == null) return 0;

            // Tính tổng số ngày dự án
            var totalDays = (project.EndDate - project.StartDate)?.TotalDays ?? 1;

            // Lấy tất cả các nhiệm vụ trong dự án
            var tasks = await _dbAnContext.Tasks
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();

            // Tính tổng số nhiệm vụ và số nhiệm vụ đã hoàn thành
            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.Status == "Hoàn thành");

            // Tính tỷ lệ hoàn thành của dự án
            var taskCompletionRate = totalTasks == 0 ? 0 : (double)completedTasks / totalTasks;

            // Tính phần trăm hoàn thành dựa trên tiến độ nhiệm vụ
            double completionPercentage = Math.Round(taskCompletionRate * 100,2);

            // Gửi nhắc nhở nếu cần thiết
            if (completionPercentage < 50) // Ví dụ: nếu tiến độ dưới 50%
            {
                // Kiểm tra thời gian còn lại
                var remainingDays = (project.EndDate?.Subtract(DateTime.Now).TotalDays) ?? 0;

                // Gửi thông báo nhắc nhở nếu dự án đang gần đến hạn (ví dụ: dưới 7 ngày)
                if (remainingDays <= 7)
                {
                    var message = $"Dự án '{project.ProjectName}' có tiến độ thấp ({completionPercentage}%) và chỉ còn {remainingDays} ngày để hoàn thành!";
                    // await SendNotificationToUsers(project.ProjectLeaderId, message); // Gửi thông báo cho người quản lý dự án
                }
            }

            return completionPercentage; // Tổng phần trăm hoàn thành
        }

        // Hàm lấy dự án của giảng viên và tính tiến độ
        public async Task<List<ProjectWithCompletion>> GetInstructorProjectsWithCompletion(int instructorId)
        {
            // Lấy tất cả các dự án của các lớp mà giảng viên dạy
            var projects = await _dbAnContext.Projects
                .Where(p => _dbAnContext.Classes
                    .Where(c => c.InstructorId == instructorId)
                    .Any(c => c.ClassId == p.ClassId))
                .ToListAsync();

            // Danh sách lưu các dự án với tiến độ hoàn thành
            var projectsWithCompletion = new List<ProjectWithCompletion>();
             // Sự chênh lệch giữa ngày kết thúc và ngày hiện tại

            // Tính tiến độ cho từng dự án
            foreach (var project in projects)
            {
                var remainingDays = (project.EndDate.HasValue ? (project.EndDate.Value - DateTime.Now).Days : 0);
                var totalDay = (project.EndDate.HasValue ? (project.EndDate.Value - project.StartDate.Value).Days : 0);

                var completionPercentage = await CalculateProjectCompletionAndSendReminder(project.ProjectId);
                projectsWithCompletion.Add(new ProjectWithCompletion
                {
                    Project = project,
                    CompletionPercentage = completionPercentage,
                    RemainingDays = remainingDays,
                    TotalDay = totalDay
                });
            }

            return projectsWithCompletion;
        }






        [HttpGet]
        public async Task<IActionResult> GetNotifications(int userId)
        {
            var notifications = await _dbAnContext.Notifications
                .Where(n => n.ReceiverId == userId) // Lấy thông báo theo người nhận
                .OrderByDescending(n => n.DateSent)
                .Join(
                    _dbAnContext.Users,        // Bảng Users
                    n => n.UserId,             // Khóa ngoại trong Notifications (người gửi)
                    u => u.UserId,             // Khóa chính trong Users
                    (n, u) => new
                    {
                        n.NotificationId,
                        n.Message,
                        n.DateSent,
                        n.IsRead,
                        SenderName = u.FullName // Lấy tên người gửi từ bảng Users
                    })
                .ToListAsync();

            return Json(notifications);
        }



        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var notification = await _dbAnContext.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _dbAnContext.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }



        /*
                public IActionResult CheckStudentCode()
                {
                    var accessToken = _contextAccessor.HttpContext.Session.GetString("AccessToken");

                    if (accessToken == null)
                    {
                        return Json(new { redirect = Url.Action("Index", "Account") });
                    }

                    var email = _contextAccessor.HttpContext.Session.GetString("Email");
                    var user = _dbAnContext.Users.FirstOrDefault(u => u.Email == email);

                    if (user == null || string.IsNullOrEmpty(user.StudentCode))
                    {
                        return Json(new { success = false });
                    }

                    return Json(new { success = true });
                }

                [HttpPost]
                public IActionResult UpdateStudentCode(string studentCode)
                {
                    var email = _contextAccessor.HttpContext.Session.GetString("Email");

                    var user = _dbAnContext.Users.FirstOrDefault(u => u.Email == email);
                    if (user != null)
                    {
                        user.StudentCode = studentCode;
                        _dbAnContext.SaveChanges();

                        // Trả về view (ví dụ như "Index" của controller "Home" hoặc một view khác)
                        return RedirectToAction("Index", "Home");
                    }

                    // Nếu không tìm thấy user hoặc cập nhật thất bại, có thể trả về một view lỗi
                    return Json(new { success = "Đã có lỗi xảy ra vui lòng thử lại sau" });// Hoặc một view khác nếu cần
                }*/



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
