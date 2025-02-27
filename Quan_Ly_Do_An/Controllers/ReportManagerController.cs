using Google.Apis.PeopleService.v1.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Quan_Ly_Do_An.Class;
using Quan_Ly_Do_An.Data;
using Quan_Ly_Do_An.Models;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static OfficeOpenXml.ExcelErrorValue;

namespace Quan_Ly_Do_An.Controllers
{
    public class ReportManagerController : Controller
    {
        private readonly IHttpContextAccessor _contextAccessor;
        //Khai báo AccessToken (lớp)
        private readonly Class.AccessToken _accessToken;
        private readonly Class.EmailService _email;
        private readonly DatabaseDoAnContext _dbAnContext;
        private readonly FileUploadService _fileUploadService;
        private readonly GeminiService _geminiService;
        private int? userId;
        private int? classId;
        private int? projectId;
        private string? email;
        private string? userName;

        public ReportManagerController(IHttpContextAccessor contextAccessor, Class.AccessToken accessToken, DatabaseDoAnContext dbAnContext, FileUploadService fileUploadService, EmailService emailService,GeminiService geminiService)
        {
            _contextAccessor = contextAccessor;
            //khởi tạo trong hàm khởi tạo
            _accessToken = accessToken;
            _dbAnContext = dbAnContext;
            _fileUploadService = fileUploadService;
            _geminiService = geminiService;
            userId = _contextAccessor.HttpContext.Session.GetInt32("UserId");
            email = _contextAccessor.HttpContext.Session.GetString("Email");
            classId = _contextAccessor.HttpContext.Session.GetInt32("classId");
            projectId = _contextAccessor.HttpContext.Session.GetInt32("ProjectId");
            userName = _contextAccessor.HttpContext.Session.GetString("UserName");
            if (!userId.HasValue || string.IsNullOrEmpty(email) || !classId.HasValue || !projectId.HasValue || string.IsNullOrEmpty(userName))
            {
                // Trả về redirect đến trang đăng xuất
                RedirectToAction("Index", "Logout");
            }
            _email = emailService;
        }
        public async Task<IActionResult> Index()
        {
            _contextAccessor.HttpContext.Session.Remove("task");
            _contextAccessor.HttpContext.Session.Remove("Tasks");
            _contextAccessor.HttpContext.Session.Remove("ListTaskRecived");
            _contextAccessor.HttpContext.Session.Remove("ProjectId");
            // Truy vấn các lớp mà giảng viên UserId đang dạy
            var query = from c in _dbAnContext.Classes
                        where c.InstructorId == userId // Lọc theo giảng viên
                        select new ClassModel
                        {
                            ClassName = c.ClassName,
                            SubjectCode = c.SubjectCode,
                            ClassId = c.ClassId
                        };
            var result = query.ToList();
            return View(result);
        }
        public async Task<IActionResult> ClassDetail(int? classId, string? className)
        {

            if (classId == null)
                return RedirectToAction("Index", "Logout"); // Hoặc trả về lỗi

            var projects = await (from c in _dbAnContext.Classes
                                  join p in _dbAnContext.Projects on c.ClassId equals p.ClassId
                                  join pt in _dbAnContext.ProjectTeams on p.ProjectId equals pt.ProjectId
                                  join u in _dbAnContext.Users on pt.UserId equals u.UserId
                                  where c.ClassId == classId
                                  select new ClassProjectViewModel
                                  {
                                      ClassId = c.ClassId,
                                      ClassName = c.ClassName,
                                      ProjectName = p.ProjectName,
                                      GroupNumber = pt.GroupNumber,
                                      ProjectLeader = u.FullName,
                                      SubjectCode = c.SubjectCode,
                                      ProjectId = p.ProjectId
                                  })
                                  .GroupBy(g => new { g.ClassId, g.ProjectId, g.GroupNumber })
                                  .Select(g => g.First())
                                  .ToListAsync();

            var groupProgressDetails = await GetProjectProgressByClass((int)classId);

            // Kiểm tra và khởi tạo GroupCompletionStats nếu không có dữ liệu
            var groupCompletionStats = await GetGroupCompletionStats((int)classId);
            var reportSubmissionStats = await GetReportSubmissionStats((int)classId);
            var getProjectStatistics = await GetProjectStatistics((int)userId,(int)classId);
            var groupProgressOverviewByTeacher = await GetGroupProgressOverviewByTeacher((int)userId, (int)classId);
            // Tạo model để truyền về view
            var viewModel = new ClassDetailViewModel
            {
                ClassId = (int)classId,
                ClassName = className ?? "Lớp chưa có tên",
                Class = projects,
                GroupProgressDetails = groupProgressDetails.Projects,
                GroupCompletionStats = groupCompletionStats,
                ReportSubmissionStats = reportSubmissionStats,// Truyền GroupCompletionStats
                ProjectStatistics = getProjectStatistics,// Truyền GroupCompletionStats
                GroupProgressView = groupProgressOverviewByTeacher,// Truyền GroupCompletionStats
            };

            return View(viewModel);
        }




        public async Task<IActionResult> GroupDetail(ClassProjectViewModel Values)
        {
            // Lấy dữ liệu liên quan đến báo cáo
            var reports = await (from pr in _dbAnContext.ProgressReports
                                 join t in _dbAnContext.Tasks on pr.TaskId equals t.TaskId
                                 join p in _dbAnContext.Projects on t.ProjectId equals p.ProjectId
                                 join c in _dbAnContext.Classes on p.ClassId equals c.ClassId
                                 join cm in _dbAnContext.ClassMembers on c.ClassId equals cm.ClassId
                                 join pt in _dbAnContext.ProjectTeams on new { p.ProjectId, cm.UserId } equals new { pt.ProjectId, pt.UserId }
                                 join u in _dbAnContext.Users on pr.UserId equals u.UserId
                                 where p.ProjectId == Values.ProjectId && c.ClassId == Values.ClassId
                                 select new ProgressReportModel
                                 {
                                     Status = pr.Status,
                                     UserName = u.FullName,
                                     ReportDate = pr.ReportDate,
                                 }).Distinct().ToListAsync();

            // Xử lý ngày và giờ báo cáo
            var reportDates = reports.Select(d => d.ReportDate?.ToString("dd-MM-yyyy") ?? "").ToList();
            var reportTimes = reports.Select(d => d.ReportDate?.ToString("hh:mm:ss tt") ?? "").ToList();

            var progressReportAndClassProject = new ProgressReportAndClassProjectViewModel
            {
                ClassProject = Values,
                ProgressReport = reports,
                ProgressReportDay = reportDates,
                ProgressReportTime = reportTimes
            };
            _contextAccessor.HttpContext.Session.SetObject("DataClass", Values);
            // Lấy dữ liệu về tiến độ dự án
            var memberProgress = await GetMemberTaskProgress(Values.ProjectId);
            var studentProgress = await GetStudentProgressByProject(Values.ProjectId);
            var projectProgress = new ProjectProgressViewModel
            {
                MemberProgress = memberProgress,
                PhaseComparison = await ComparePhaseProgress(Values.ProjectId),
                GroupProgress = await GetGroupProgress(Values.ProjectId),
                UserProgressView = await GroupProgress(Values.ProjectId),
                ProjectIssuesView = await CheckProjectIssues(Values.ProjectId),
                ProgressReportView = await GetListTaskReportData(Values.ProjectId),
                GetProjectPhases =  GetProjectPhases(Values.ProjectId),
            };

            // Tạo CombinedViewModel và trả về
            var model = new CombinedViewModel
            {
                ProjectProgress = projectProgress,
                ProgressReportAndClassProject = progressReportAndClassProject
            };

            return View(model);
        }

        private HashSet<string> generatedColors = new HashSet<string>();
        private string GenerateRandomColor()
        {
            Random random = new Random();
            string newColor;

            // Lặp lại cho đến khi màu mới không trùng với màu đã tạo
            do
            {
                newColor = $"#{random.Next(0x1000000):X6}";
            }
            while (generatedColors.Contains(newColor)); // Kiểm tra sự trùng lặp

            // Thêm màu mới vào HashSet để theo dõi
            generatedColors.Add(newColor);

            return newColor;
        }
        private  List<ProjectPhaseViewModel> GetProjectPhases(int projectId)
        {
            // Lấy dữ liệu từ cơ sở dữ liệu trước
            var phases =  _dbAnContext.ProjectMilestones
                .Where(p => p.ProjectId == projectId)
                .AsEnumerable() // Đảm bảo phần còn lại xử lý ở phía client
                .Select(p => new
                {
                    p.StageName,
                    StartDate = p.StartDate.ToDateTime(TimeOnly.MinValue), // Chuyển đổi ở phía client
                    EndDate = p.EndDate.ToDateTime(TimeOnly.MinValue) // Chuyển đổi ở phía client
                })
                .OrderBy(p => p.StartDate) // Sắp xếp sau khi chuyển đổi
                .ToList();

            // Tính tổng số ngày
            int totalDays = phases.Sum(p => (p.EndDate - p.StartDate).Days);

            // Chuẩn bị danh sách ProjectPhaseViewModel
            var phaseModels = phases.Select(p => new ProjectPhaseViewModel
            {
                StageName = p.StageName,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                TotalDays = (p.EndDate - p.StartDate).Days,
                Percentage = totalDays > 0 ? ((p.EndDate - p.StartDate).Days * 100) / totalDays : 0,
                BackgroundColor = GenerateRandomColor()
            }).ToList();

            return phaseModels;
        }
        public async Task<List<StudentProgressViewModel>> GetStudentProgressByProject(int projectId)
        {
            var studentProgress = await (from pt in _dbAnContext.ProjectTeams
                                         join u in _dbAnContext.Users on pt.UserId equals u.UserId
                                         join t in _dbAnContext.Tasks on pt.ProjectId equals t.ProjectId into tasks
                                         from task in tasks.DefaultIfEmpty()
                                         join pr in _dbAnContext.ProgressReports on task.TaskId equals pr.TaskId into reports
                                         from report in reports.DefaultIfEmpty()
                                         where pt.ProjectId == projectId
                                         group new { pt, u, task, report } by new { pt.UserId, u.FullName } into g
                                         select new StudentProgressViewModel
                                         {
                                             StudentName = g.Key.FullName,
                                             TotalTasksAssigned = g.Count(x => x.task != null && x.task.AssignedToUserId == g.Key.UserId),
                                             TotalTasksCompleted = g.Count(x => x.report != null && x.report.Status == "Đã phê duyệt"),
                                             TotalReportsSubmitted = g.Count(x => x.report != null && x.report.Status == "Đã Nộp"),
                                             TotalReportsFailed = g.Count(x => x.report != null && x.report.Status == "Đã từ chối"),
                                             CompletionPercentage = g.Count(x => x.task != null) == 0
                                                 ? 0
                                                 : (int)(g.Count(x => x.report != null && x.report.Status == "Đã phê duyệt") /
                                                        (double)g.Count(x => x.task != null) * 100)
                                         }).ToListAsync();

            return studentProgress;
        }
        public async Task<List<GroupProgressViewModel>> GetGroupProgressOverviewByTeacher(int teacherId,int classId)
        {
            var groupProgress = await (from p in _dbAnContext.Projects
                                       join c in _dbAnContext.Classes on p.ClassId equals c.ClassId
                                       join t in _dbAnContext.Tasks on p.ProjectId equals t.ProjectId into tasks
                                       from task in tasks.DefaultIfEmpty()
                                       join pr in _dbAnContext.ProgressReports on task.TaskId equals pr.TaskId into reports
                                       from report in reports.DefaultIfEmpty()
                                       where c.InstructorId == teacherId && c.ClassId== classId // Lọc theo giảng viên
                                       group new { p, task, report } by new { p.ProjectId, p.ProjectName } into g
                                       select new GroupProgressViewModel
                                       {
                                           ProjectName = g.Key.ProjectName,

                                           // Tổng số nhiệm vụ đã tạo
                                           TotalTasksCreated = g.Select(x => x.task.TaskId).Distinct().Count(),

                                           // Tổng số báo cáo đã nộp
                                           TotalReportsSubmitted = g.Count(x => x.report != null && x.report.Status.Trim() == "Đã Nộp"),

                                           // Tổng số báo cáo hoàn thiện
                                           TotalReportsCompleted = g.Count(x => x.report != null && x.report.Status.Trim() == "Đã phê duyệt"),

                                           // Tính phần trăm hoàn thành
                                           CompletionPercentage1 = g.Select(x => x.task.TaskId).Distinct().Count() > 0
                                               ? (int)((g.Count(x => x.report != null && x.report.Status.Trim() == "Đã phê duyệt") /
                                                        (double)g.Select(x => x.task.TaskId).Distinct().Count()) * 100)
                                               : 0
                                       }).ToListAsync();

            return groupProgress;
        }

        public async Task<DashboardStatisticsViewModel> GetProjectStatistics(int teacherId, int classId)
        {
            // Kiểm tra giảng viên có phụ trách lớp này không
            var isTeacherResponsible = await _dbAnContext.Classes
                                                         .AnyAsync(c => c.ClassId == classId && c.InstructorId == teacherId);
            if (!isTeacherResponsible)
            {
                throw new UnauthorizedAccessException("Giảng viên không phụ trách lớp này.");
            }

            // Lọc danh sách các dự án theo classId
            var projectIds = await _dbAnContext.Projects
                                               .Where(p => p.ClassId == classId)
                                               .Select(p => p.ProjectId)
                                               .ToListAsync();

            // Tổng số dự án trong lớp
            var totalProjects = projectIds.Count;

            // Tổng số sinh viên tham gia các dự án
            var totalStudents = await _dbAnContext.ProjectTeams
                                                  .Where(pt => projectIds.Contains(pt.ProjectId))
                                                  .Select(pt => pt.UserId)
                                                  .Distinct()
                                                  .CountAsync();

            // Tính tỷ lệ hoàn thành trung bình các dự án trong lớp
            var completionRates = await _dbAnContext.Projects
                                                    .Where(p => p.ClassId == classId)
                                                    .Select(p => new
                                                    {
                                                        ProjectId = p.ProjectId,
                                                        CompletionRate = (double)_dbAnContext.ProgressReports
                                                            .Where(pr => _dbAnContext.Tasks
                                                                  .Where(t => t.ProjectId == p.ProjectId)
                                                                  .Select(t => t.TaskId)
                                                                  .Contains(pr.TaskId)
                                                                  && pr.Status == "Đã phê duyệt")
                                                            .Count() /
                                                            (_dbAnContext.Tasks.Count(t => t.ProjectId == p.ProjectId) > 0
                                                                ? _dbAnContext.Tasks.Count(t => t.ProjectId == p.ProjectId)
                                                                : 1)
                                                    })
                                                    .ToListAsync();

            var averageCompletionRate = completionRates.Any()
                ? completionRates.Average(x => x.CompletionRate)
                : 0;

            // Tổng số báo cáo đã phê duyệt
            var totalReportsApproved = await _dbAnContext.ProgressReports
                                                         .Where(pr => _dbAnContext.Tasks
                                                                              .Where(t => projectIds.Contains(t.ProjectId))
                                                                              .Select(t => t.TaskId)
                                                                              .Contains(pr.TaskId)
                                                             && pr.Status == "Đã phê duyệt")
                                                         .CountAsync();

            // Tổng số báo cáo bị từ chối
            var totalReportsRejected = await _dbAnContext.ProgressReports
                                                         .Where(pr => _dbAnContext.Tasks
                                                                              .Where(t => projectIds.Contains(t.ProjectId))
                                                                              .Select(t => t.TaskId)
                                                                              .Contains(pr.TaskId)
                                                             && pr.Status == "Đã từ chối")
                                                         .CountAsync();
            // Trả về mô hình dữ liệu tổng quan mà không có chi tiết dự án
            return new DashboardStatisticsViewModel
            {
                TotalProjects = totalProjects,
                TotalStudents = totalStudents,
                AverageCompletionRate = Math.Round(averageCompletionRate * 100, 2),
                TotalReportsApproved = totalReportsApproved,
                TotalReportsRejected = totalReportsRejected,
                ProjectDetails = null // Remove the ProjectDetails
            };
        }



        public async Task<List<ProgressReportModel>> GetListTaskReportData(int projectId)
        {
            var now = DateTime.Now;

            // Lấy danh sách các thành viên trong nhóm (dự án)
            var projectMembers = await _dbAnContext.ProjectTeams
                .Where(pm => pm.ProjectId == projectId)
                .Select(pm => pm.UserId)
                .Distinct() // Đảm bảo rằng mỗi thành viên chỉ xuất hiện một lần
                .ToListAsync();

            // Truy vấn tất cả báo cáo của các thành viên trong nhóm
            var allReports = await _dbAnContext.ProgressReports
                .Where(pr => projectMembers.Contains((int)pr.UserId))  // Lọc theo thành viên trong dự án
                .OrderBy(pr => pr.ReportDate)  // Sắp xếp theo ngày báo cáo
                .GroupBy(pr => pr.UserId) // Nhóm theo UserId để tránh trùng lặp
                .Select(group => new ProgressReportModel
                {
                    ReportId = group.FirstOrDefault().ProgressReportId,
                    AttachedFilePath = group.FirstOrDefault().AttachedFile,
                    ReportDate = group.FirstOrDefault().ReportDate,
                    WorkDescription = group.FirstOrDefault().WorkDescription,
                    Status=group.FirstOrDefault().Status,
                    // Lấy thông tin người dùng đồng bộ ngoài query
                    UserName = _dbAnContext.Users.Where(u => u.UserId == group.Key).Select(u => u.FullName).FirstOrDefault(),
                    Email = _dbAnContext.Users.Where(u => u.UserId == group.Key).Select(u => u.Email).FirstOrDefault()
                })
                .ToListAsync();

            return allReports;
        }



        public async Task<GroupProgressViewModel> GetGroupProgress(int projectId)
        {
            try
            {
                var taskSummary = await (from t in _dbAnContext.Tasks
                                         where t.ProjectId == projectId
                                         select new
                                         {
                                             TaskId = t.TaskId,
                                             IsAssigned = t.AssignedToUserId != null,
                                             SubTaskCount = _dbAnContext.SubTasks
                                                 .Where(st => st.TaskId == t.TaskId)
                                                 .Select(st => st.SubTaskId)
                                                 .Distinct()
                                                 .Count(),
                                             ReportsSubmitted = _dbAnContext.ProgressReports
                                                 .Where(pr => pr.TaskId == t.TaskId && pr.Status == "Đã Nộp")
                                                 .Select(pr => pr.ProgressReportId)
                                                 .Distinct()
                                                 .Count(),
                                             ReportsCompleted = _dbAnContext.ProgressReports
                                                 .Where(pr => pr.TaskId == t.TaskId && pr.Status == "Đã phê duyệt")
                                                 .Select(pr => pr.ProgressReportId)
                                                 .Distinct()
                                                 .Count(),
                                             ReportsRejected = _dbAnContext.ProgressReports
                                                 .Where(pr => pr.TaskId == t.TaskId && pr.Status == "Đã từ chối")
                                                 .Select(pr => pr.ProgressReportId)
                                                 .Distinct()
                                                 .Count()
                                         }).ToListAsync();

                var viewModel = new GroupProgressViewModel
                {
                    TotalTasksCreated = taskSummary.Count,
                    TotalTasksAssigned = taskSummary.Count(x => x.IsAssigned),
                    TotalSubTasksCreated = taskSummary.Sum(x => x.SubTaskCount),
                    TotalReportsSubmitted = taskSummary.Sum(x => x.ReportsSubmitted),
                    TotalReportsCompleted = taskSummary.Sum(x => x.ReportsCompleted),
                    TotalReportsFail = taskSummary.Sum(x => x.ReportsRejected),
                    TotalTasksCompleted = taskSummary.Count(x => x.ReportsCompleted > 0)
                };

                // Tính tỷ lệ hoàn thành báo cáo
                var completionRate = (double)viewModel.TotalReportsCompleted / viewModel.TotalReportsSubmitted * 100;
                // Tính tỷ lệ hoàn thành nhiệm vụ
                var taskCompletionRate = (double)viewModel.TotalTasksCompleted / viewModel.TotalTasksCreated * 100;

                // Gán giá trị tỷ lệ hoàn thành vào viewModel
                viewModel.CompletionRate = completionRate.ToString("F2");  // Hoặc lưu vào viewModel tùy nhu cầu
                viewModel.TaskCompletionRate = taskCompletionRate.ToString("F2");

                return viewModel;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                throw;
            }
        }

        //biểu đồ
        public async Task<List<UserProgressViewModel>> GroupProgress(int projectId)
        {
            try
            {
                // Truy vấn nhiệm vụ và người dùng
                var taskQuery = await _dbAnContext.Tasks
                    .Where(t => t.ProjectId == projectId)
                    .Join(_dbAnContext.Users,
                        t => t.AssignedToUserId,
                        u => u.UserId,
                        (t, u) => new
                        {
                            t.TaskId,
                            u.UserId,
                            u.FullName // Lấy tên người dùng từ bảng Users
                        })
                    .ToListAsync();

                // Lấy danh sách tên người dùng mà không cần phải nhóm
                var userNames = taskQuery
                    .Select(t => t.FullName)  // Lấy danh sách tên người dùng
                    .Distinct()               // Loại bỏ tên trùng
                    .ToList();

                // Truy vấn báo cáo tiến độ
                var reportQuery = await _dbAnContext.ProgressReports
                    .Where(pr => pr.Status == "Đã Nộp" || pr.Status == "Đã phê duyệt" || pr.Status == "Đã từ chối" || pr.Status == "Đang tiến hành")
                    .Select(pr => new
                    {
                        pr.TaskId,
                        pr.Status
                    })
                    .ToListAsync();

                // Kết hợp nhiệm vụ với báo cáo
                var taskWithReports = taskQuery
                    .GroupJoin(reportQuery,
                        t => t.TaskId,
                        pr => pr.TaskId,
                        (t, reports) => new
                        {
                            t.UserId,
                            t.FullName,  // Lấy FullName để sử dụng trong ViewModel
                            Reports = reports.ToList()
                        })
                    .ToList();

                // Nhóm và tính toán số liệu theo người dùng
                var groupedData = taskWithReports
                    .GroupBy(t => t.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        FullName = g.FirstOrDefault()?.FullName,  // Lấy tên người dùng
                        ReportsSubmitted = g.Sum(x => x.Reports.Count(r => r.Status == "Đã Nộp")),
                        ReportsCompleted = g.Sum(x => x.Reports.Count(r => r.Status == "Đã phê duyệt")),
                        ReportsRejected = g.Sum(x => x.Reports.Count(r => r.Status == "Đã từ chối")),
                        ReportsInProgress = g.Sum(x => x.Reports.Count(r => r.Status == "Đang tiến hành")),
                        TotalTasksAssigned = g.Count(),
                        CompletionRate = g.Count() > 0
                            ? (double)g.Sum(x => x.Reports.Count(r => r.Status == "Đã phê duyệt")) / g.Count() * 100
                            : 0
                    })
                    .ToList();

                // Chuyển thành ViewModel
                var viewModel = groupedData.Select(user => new UserProgressViewModel
                {
                    UserId = user.UserId,
                    UserName = user.FullName,  // Hiển thị tên người dùng
                    ReportsSubmitted = user.ReportsSubmitted,
                    ReportsCompleted = user.ReportsCompleted,
                    ReportsRejected = user.ReportsRejected,
                    ReportsInProgress = user.ReportsInProgress,
                    TotalTasksAssigned = user.TotalTasksAssigned,
                    CompletionRate = user.CompletionRate
                }).ToList();

                return viewModel;
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                Console.WriteLine(ex.Message);
                return null;
            }
        }
        private async Task<List<MemberTaskProgressViewModel>> GetMemberTaskProgress(int projectId)
        {
            var taskProgress = await (from t in _dbAnContext.Tasks
                                      join st in _dbAnContext.SubTasks on t.TaskId equals st.TaskId
                                      join m in _dbAnContext.Users on st.AssignedToUserId equals m.UserId
                                      where t.ProjectId == projectId
                                      group new { t, st, m } by new { m.UserId, m.FullName } into g
                                      select new MemberTaskProgressViewModel
                                      {
                                          MemberName = g.Key.FullName,
                                          AssignedTo = g.Key.UserId,
                                          TotalTasks = g.Select(x => x.t.TaskId).Distinct().Count(),
                                          TotalSubTasks = g.Count(), // Tổng số công việc phụ
                                          CompletedSubTasks = g.Count(x => x.st.Status == "Hoàn thành"), // Công việc phụ hoàn thành
                                      }).ToListAsync();

            // Tính tỷ lệ tiến độ cho mỗi thành viên
            taskProgress.ForEach(tp =>
            {
                tp.ProgressPercentage = tp.TotalSubTasks == 0 ? 0 : (int)((double)tp.CompletedSubTasks / tp.TotalSubTasks * 100);
            });

            return taskProgress;
        }

        private async Task<List<ProjectPhaseComparisonViewModel>> ComparePhaseProgress(int projectId)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            // Lấy thông tin các giai đoạn trong dự án
            var phases = await _dbAnContext.ProjectMilestones
                .Where(p => p.ProjectId == projectId)
                .Select(p => new
                {
                    p.MilestoneId,
                    p.StageName,
                    p.StartDate,
                    p.EndDate,
                    PlannedDays = EF.Functions.DateDiffDay(p.StartDate, p.EndDate),
                    // Cập nhật logic tính DaysElapsed
                    DaysElapsed = p.StartDate > today
                        ? 0 // Nếu ngày bắt đầu trong tương lai, DaysElapsed = 0
                        : EF.Functions.DateDiffDay(p.StartDate, today), // Nếu ngày bắt đầu trong quá khứ hoặc hôm nay, tính bình thường
                    TotalSubTasks = _dbAnContext.SubTasks
                        .Where(st => st.Task.ProjectId == projectId
                                     && st.StartDate.HasValue
                                     && DateOnly.FromDateTime(st.StartDate.Value) >= p.StartDate
                                     && DateOnly.FromDateTime(st.StartDate.Value) <= p.EndDate)
                        .Count(),
                    CompletedSubTasks = _dbAnContext.SubTasks
                        .Where(st => st.Task.ProjectId == projectId
                                     && st.StartDate.HasValue
                                     && DateOnly.FromDateTime(st.StartDate.Value) >= p.StartDate
                                     && DateOnly.FromDateTime(st.StartDate.Value) <= p.EndDate
                                     && st.Status == "Hoàn thành")
                        .Count()
                })
                .ToListAsync();

            // Tính toán tiến độ thực tế và trạng thái giai đoạn
            return phases.Select(p =>
            {
                // Tính tiến độ thực tế dựa trên số lượng nhiệm vụ hoàn thành
                var actualProgress = p.TotalSubTasks == 0 ? 0 : (int)((double)p.CompletedSubTasks / p.TotalSubTasks * 100);

                // Tính tiến độ đã trôi qua dựa trên số ngày đã qua so với số ngày kế hoạch
                var elapsedProgress = p.PlannedDays == 0 || p.DaysElapsed <= 0 ? 0 : (int)((double)p.DaysElapsed / p.PlannedDays * 100);

                // Đánh giá trạng thái dựa trên tiến độ thực tế và tiến độ đã trôi qua
                var status = p.TotalSubTasks == 0
    ? "not_started" // Nếu không có công việc nào, trạng thái là "not_started" hoặc "pending"
    : actualProgress == 100
        ? "success" // Nếu tiến độ thực tế là 100%, thì trạng thái là "success"
        : actualProgress >= elapsedProgress
            ? "success" // Tiến độ thực tế >= tiến độ đã trôi qua
            : actualProgress >= elapsedProgress / 2
                ? "warning" // Tiến độ thực tế >= 50% tiến độ đã trôi qua
                : "danger"; // Tiến độ thực tế thấp hơn tiến độ đã trôi qua


                return new ProjectPhaseComparisonViewModel
                {
                    MilestoneId = p.MilestoneId,
                    StageName = p.StageName,
                    PlannedDays = p.PlannedDays,
                    ActualProgress = actualProgress,
                    Status = status,
                    DaysElapsed = p.DaysElapsed,
                    TotalSubTasks = p.TotalSubTasks,
                    CompletedSubTasks = p.CompletedSubTasks
                };
            }).ToList();
        }

        public async Task<ProjectIssuesViewModel> CheckProjectIssues(int projectId)
        {
            var now = DateTime.Now;
            var today = new DateTime(now.Year, now.Month, now.Day);

            // Lấy danh sách các thành viên trong nhóm (dự án)
            var projectMembers = await _dbAnContext.ProjectTeams
                .Where(pm => pm.ProjectId == projectId)
                .Select(pm => pm.UserId)
                .ToListAsync();

            // 1. Nhiệm vụ có hạn nộp sắp đến hoặc đã trễ
            var tasksWithDeadlineIssues = await _dbAnContext.Tasks
                .Where(t => t.EndDate.HasValue
                            && projectMembers.Contains((int)t.AssignedToUserId)) // Lọc theo thành viên trong dự án
                .Select(t => new TaskIssueViewModel
                {
                    TaskId = t.TaskId,
                    TaskName = t.TaskName,
                    EndDate = t.EndDate.Value,
                    Status = t.Status,
                    // Kiểm tra xem nhiệm vụ đã hoàn thành chưa
                    UpdatedStatus = t.Status == "Hoàn thành" ? "Đã hoàn thành" :
                        (t.EndDate.Value < today ? "Đã quá hạn" :
                        (t.EndDate.Value <= today.AddDays(3) ? "Sắp hết hạn" : "Còn hạn")),

                    // Tính số ngày đếm ngược
                    DaysRemaining = t.Status == "Hoàn thành" ? 0 : (t.EndDate.Value - today).Days
                })
                .ToListAsync();

            // 2. Thành viên không cập nhật tiến độ định kỳ
            var recentReportUserIds = _dbAnContext.ProgressReports
                .Where(pr => pr.ReportDate >= today.AddDays(-7))
                .Select(pr => pr.UserId)
                .Distinct();

            var membersNotUpdatingProgress = await _dbAnContext.Users
                .Where(u => projectMembers.Contains(u.UserId) && !recentReportUserIds.Contains(u.UserId))
                .Select(u => new MemberIssueViewModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email
                })
                .ToListAsync();

            // 3. Thành viên không nhận bất kỳ nhiệm vụ nào
            var membersWithoutTasks = await _dbAnContext.Users
                .Where(u => projectMembers.Contains(u.UserId) && !_dbAnContext.Tasks.Any(t => t.AssignedToUserId == u.UserId))
                .Select(u => new MemberIssueViewModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email
                })
                .ToListAsync();

            // Kết quả trả về với trạng thái đã thay đổi trên View
            return new ProjectIssuesViewModel
            {
                TasksWithDeadlineIssues = tasksWithDeadlineIssues,
                MembersNotUpdatingProgress = membersNotUpdatingProgress,
                MembersWithoutTasks = membersWithoutTasks
            };
        }
        //thống kê theo lớp
        public async Task<ClassProjectProgressViewModel> GetProjectProgressByClass(int classId)
        {
            try
            {
                var projects = await _dbAnContext.Projects
                .Where(p => p.ClassId == classId)
                .Select(p => new
                {
                    p.ProjectId,
                    p.ProjectName,
                    Groups = _dbAnContext.ProjectTeams
                        .Where(t => t.ProjectId == p.ProjectId)
                        .GroupBy(t => t.GroupNumber)
                        .Select(g => new
                        {
                            GroupNumber = g.Key,
                            Members = g.Select(m => m.UserId).ToList(),
                            TaskProgress = _dbAnContext.Tasks
                                .Where(t => t.ProjectId == p.ProjectId)
                                .GroupBy(t => t.Status)
                                .Select(t => new
                                {
                                    Status = t.Key,
                                    Count = t.Count()
                                }).ToList()
                        }).ToList()
                }).ToListAsync();

                var progressData = projects.Select(p => new ProjectProgressDetail
                {
                    ProjectName = p.ProjectName,
                    Groups = p.Groups.Select(g => new GroupProgressDetail
                    {
                        GroupNumber = g.GroupNumber,
                        TotalTasks = g.TaskProgress.Sum(tp => tp.Count),
                        CompletedTasks = g.TaskProgress.Where(tp => tp.Status == "Hoàn thành").Sum(tp => tp.Count),
                        InProgressTasks = g.TaskProgress.Where(tp => tp.Status == "Đang thực hiện").Sum(tp => tp.Count),
                        PendingTasks = g.TaskProgress.Where(tp => tp.Status == "Đang chờ").Sum(tp => tp.Count),
                        // Tính toán phần trăm tiến độ cho nhóm
                        ProgressPercentage = CalculateProgressPercentage(
                            g.TaskProgress.Where(tp => tp.Status == "Hoàn thành").Sum(tp => tp.Count),
                            g.TaskProgress.Sum(tp => tp.Count)
                        )
                    }).ToList()
                }).ToList();

                return new ClassProjectProgressViewModel
                {
                    ClassId = classId,
                    ClassName = await _dbAnContext.Classes
                        .Where(c => c.ClassId == classId)
                        .Select(c => c.ClassName)
                        .FirstOrDefaultAsync(),
                    Projects = progressData
                };
            }
            catch (Exception ex)
            {
                var a = ex.Message;  // Bạn có thể log lỗi ở đây
            }

            return null;
        }

        private double CalculateProgressPercentage(int completedTasks, int totalTasks)
        {
            if (totalTasks == 0)
                return 0; // Tránh chia cho 0, trả về 0% nếu không có nhiệm vụ

            return (double)completedTasks / totalTasks * 100;
        }

        public async Task<GroupCompletionStatsViewModel> GetGroupCompletionStats(int classId)
        {
            var projects = await _dbAnContext.Projects
                .Where(p => p.ClassId == classId)
                .Select(p => new
                {
                    p.ProjectId,
                    Groups = _dbAnContext.ProjectTeams
                        .Where(g => g.ProjectId == p.ProjectId)
                        .GroupBy(g => g.GroupNumber)
                        .Select(g => new
                        {
                            GroupNumber = g.Key,
                            TotalTasks = _dbAnContext.Tasks
                                .Where(t => t.ProjectId == p.ProjectId)
                                .Count(),
                            CompletedTasks = _dbAnContext.Tasks
                                .Where(t => t.ProjectId == p.ProjectId && t.Status == "Completed")
                                .Count()
                        }).ToList()
                }).ToListAsync();

            int completedGroups = 0, incompleteGroups = 0;

            foreach (var project in projects)
            {
                foreach (var group in project.Groups)
                {
                    if (group.TotalTasks > 0 && group.TotalTasks == group.CompletedTasks)
                        completedGroups++;
                    else
                        incompleteGroups++;
                }
            }

            return new GroupCompletionStatsViewModel
            {
                CompletedGroups = completedGroups,
                IncompleteGroups = incompleteGroups
            };
        }
        public async Task<ReportSubmissionStatsViewModel> GetReportSubmissionStats(int classId)
        {
            // Lấy danh sách thành viên trong lớp
            var classMembers = await _dbAnContext.ClassMembers
                .Where(cm => cm.ClassId == classId)
                .Select(cm => cm.UserId)
                .ToListAsync();

            // Lấy tất cả báo cáo của các thành viên trong lớp
            var reports = await _dbAnContext.ProgressReports
                .Where(r => classMembers.Contains((int)r.UserId))
                .ToListAsync();

            // Lấy ngày kết thúc nhiệm vụ của tất cả các nhiệm vụ trong lớp
            var taskEndDates = await _dbAnContext.Tasks
                .Where(t => classMembers.Contains((int)t.AssignedToUserId))
                .Select(t => new { t.TaskId, t.EndDate })
                .ToListAsync();

            var totalReports = reports.Count;
            var onTimeReports = 0;
            var lateReports = 0;

            // Lặp qua các báo cáo để kiểm tra tình trạng nộp đúng hạn và nộp trễ
            foreach (var report in reports)
            {
                var taskEndDate = taskEndDates
                    .FirstOrDefault(t => t.TaskId == report.TaskId)?.EndDate;

                if (taskEndDate.HasValue)
                {
                    // Nếu báo cáo nộp đúng hạn (ngày nộp <= ngày kết thúc nhiệm vụ)
                    if (report.Status == "Đã phê duyệt" && report.ReportDate <= taskEndDate.Value)
                    {
                        onTimeReports++;
                    }
                    else
                    {
                        lateReports++;
                    }
                }
            }

            return new ReportSubmissionStatsViewModel
            {
                TotalReports = totalReports,
                OnTimeReports = onTimeReports,
                LateReports = lateReports
            };
        }


        [HttpPost]
        public async Task<IActionResult> SendReminderEmail(string email, string fullName, int userId)
        {
            var userIdTeacher = _contextAccessor.HttpContext.Session.GetInt32("UserId");

            // Kiểm tra xem userId của người nhận có tồn tại trong bảng Users không
            var receiverUser = await _dbAnContext.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (receiverUser == null)
            {
                return BadRequest("Người nhận không tồn tại trong hệ thống.");
            }

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email không được để trống.");
            }

            string subject = $"Giảng viên {userIdTeacher} nhắc nhở cập nhật tiến độ định kỳ";
            string body = $"<p>Xin chào {fullName},</p>" +
                          "<p>Bạn chưa cập nhật tiến độ định kỳ của mình. Vui lòng đăng nhập vào hệ thống để thực hiện cập nhật.</p>" +
                          "<p>Trân trọng,</p>" +
                          "<p>Quản lý dự án</p>";

            try
            {
                // Thực hiện gửi email (Nếu có phần gửi email, hãy thêm vào đây)
                
                // Thêm thông báo vào bảng Notifications
                var notification = new Notification
                {
                    UserId = (int)userIdTeacher,
                    Message = "Bạn đã nhận được email nhắc nhở cập nhật tiến độ định kỳ.",
                    DateSent = DateTime.Now,
                    IsRead = false,
                    ReceiverId = userId,  // Lưu thông báo cho người nhận
                };

                _dbAnContext.Notifications.Add(notification);
                await _dbAnContext.SaveChangesAsync();

                return Ok("Email nhắc nhở đã được gửi thành công.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi gửi email hoặc lưu thông báo: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> GenerateContent([FromBody] AnswersRequest request)
        {
            var values = _contextAccessor.HttpContext.Session.GetObject<ClassProjectViewModel>("DataClass");
            if (string.IsNullOrWhiteSpace(request.Answers))
            {
                return Json(new { success = false, response = "Câu trả lời không được để trống." });
            }

            try
            {
                // Lấy dữ liệu về tiến độ dự án
                var projectProgress = await BuildProjectProgress(values.ProjectId);
                var question = BuildQuestion(request.Answers, projectProgress);
                var responses = await _geminiService.GenerateContentAsync(question);

                TempData["response"] = responses;
                return Json(new { success = true, response = responses });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, response = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // Tách phần xử lý dữ liệu dự án
        private async Task<ProjectProgressViewModel> BuildProjectProgress(int projectId)
        {
            return new ProjectProgressViewModel
            {
                MemberProgress = await GetMemberTaskProgress(projectId),
                PhaseComparison = await ComparePhaseProgress(projectId),
                GroupProgress = await GetGroupProgress(projectId),
                UserProgressView = await GroupProgress(projectId),
                ProjectIssuesView = await CheckProjectIssues(projectId),
                ProgressReportView = await GetListTaskReportData(projectId),
                GetProjectPhases = GetProjectPhases(projectId),
            };
        }

        // Tạo câu hỏi dựa trên câu trả lời và tiến độ
        private string BuildQuestion(string answers, ProjectProgressViewModel projectProgress)
        {
            var reportData = GenerateReport(projectProgress);
            return answers.Equals("Analysis", StringComparison.OrdinalIgnoreCase)
                ? "xây dựng một báo cáo chi tiết, hoàn chỉnh và chuyên nghiệp với dữ liệu sau.\n" + reportData
                : answers + " dựa vào dữ liệu sau *chú ý:trả lời trực quan và ngắn gọn nhất,NẾU CÂU HỎI KHÔNG LIÊN QUAN ĐẾN DỮ LIỆU HÃY TRẢ LỜI 'CÂU HỎI CỦA BẠN KHÔNG HỢP LỆ' \n" + reportData;
        }

        public string GenerateReport(ProjectProgressViewModel projectProgress)
        {
            // Khởi tạo báo cáo
            var Values = _contextAccessor.HttpContext.Session.GetObject<ClassProjectViewModel>("DataClass");
            // Khởi tạo báo cáo
            var reportBuilder = new StringBuilder();

            // Thông tin chung
            reportBuilder.AppendLine($"{Values.ClassName} - Lớp: {Values.SubjectCode}");
            reportBuilder.AppendLine($"Tên đề tài: {Values.ProjectName} - Nhóm: {Values.GroupNumber}");
            reportBuilder.AppendLine($"Trưởng nhóm: {Values.ProjectLeader}");
            reportBuilder.AppendLine();

            // Bảng tiến độ tổng thể
            reportBuilder.AppendLine("Bảng tiến độ tổng thể");
            reportBuilder.AppendLine("Chỉ số\tGiá trị");
            reportBuilder.AppendLine($"Tổng số nhiệm vụ đã tạo\t{(projectProgress.GroupProgress.TotalTasksCreated > 0 ? projectProgress.GroupProgress.TotalTasksCreated.ToString() : "Chưa có")}");
            reportBuilder.AppendLine($"Tổng số nhiệm vụ đã nhận\t{(projectProgress.GroupProgress.TotalTasksAssigned > 0 ? projectProgress.GroupProgress.TotalTasksAssigned.ToString() : "Chưa có")}");
            reportBuilder.AppendLine($"Tổng số công việc phụ đã tạo\t{(projectProgress.GroupProgress.TotalSubTasksCreated > 0 ? projectProgress.GroupProgress.TotalSubTasksCreated.ToString() : "Chưa có")}");
            reportBuilder.AppendLine($"Tổng số báo cáo tiến độ đã nộp\t{(projectProgress.GroupProgress.TotalReportsSubmitted > 0 ? projectProgress.GroupProgress.TotalReportsSubmitted.ToString() : "Chưa có")}");
            reportBuilder.AppendLine($"Tổng số báo cáo hoàn thiện đã nộp\t{(projectProgress.GroupProgress.TotalReportsCompleted > 0 ? projectProgress.GroupProgress.TotalReportsCompleted.ToString() : "Chưa có")}");
            reportBuilder.AppendLine($"Tổng số báo cáo bị từ chối\t{(projectProgress.GroupProgress.TotalReportsFail > 0 ? projectProgress.GroupProgress.TotalReportsFail.ToString() : "Chưa có")}");
            reportBuilder.AppendLine($"Tỷ lệ hoàn thành\t{(projectProgress.GroupProgress.CompletionPercentage >= 0 ? projectProgress.GroupProgress.CompletionPercentage.ToString() : "Chưa có")}%");
            reportBuilder.AppendLine($"Tỷ lệ hoàn thành báo cáo\t{(projectProgress.GroupProgress.ReportCompletionPercentage >= 0 ? projectProgress.GroupProgress.ReportCompletionPercentage.ToString() : "Chưa có")}%");
            reportBuilder.AppendLine($"Tỷ lệ hoàn thành nhiệm vụ\t{(projectProgress.GroupProgress.TaskCompletionRate == "0" ? projectProgress.GroupProgress.TaskCompletionRate.ToString() : "0")}%");
            reportBuilder.AppendLine();

            // So sánh tiến độ giai đoạn
            reportBuilder.AppendLine("So sánh tiến độ giai đoạn");
            reportBuilder.AppendLine("Tên giai đoạn\tSố ngày kế hoạch\tTiến độ thực tế (%)\tSố ngày đã trôi qua\tTổng số công việc\tSố công việc đã hoàn thành\tTrạng thái");

            foreach (var phase in projectProgress.PhaseComparison)
            {
                reportBuilder.AppendLine($"{(string.IsNullOrEmpty(phase.StageName) ? "Chưa có" : phase.StageName)}\t" +
                                         $"{(phase.PlannedDays > 0 ? phase.PlannedDays.ToString() : "Chưa có")}\t" +
                                         $"{(phase.ActualProgress >= 0 ? phase.ActualProgress.ToString() : "Chưa có")}%\t" +
                                         $"{(phase.DaysElapsed > 0 ? phase.DaysElapsed.ToString() : "Chưa có")}\t" +
                                         $"{(phase.TotalSubTasks > 0 ? phase.TotalSubTasks.ToString() : "Chưa có")}\t" +
                                         $"{(phase.CompletedSubTasks > 0 ? phase.CompletedSubTasks.ToString() : "Chưa có")}\t" +
                                         $"{(string.IsNullOrEmpty(phase.Status) ? "Chưa có" : phase.Status)}");
            }
            reportBuilder.AppendLine();

            // Tiến độ công việc của các thành viên
            reportBuilder.AppendLine("Tiến độ công việc của các thành viên");
            reportBuilder.AppendLine("Tên thành viên\tSố nhiệm vụ\tTổng số công việc phụ\tCông việc phụ hoàn thành\tPhần trăm tiến độ");

            foreach (var member in projectProgress.MemberProgress)
            {
                reportBuilder.AppendLine($"{(string.IsNullOrEmpty(member.MemberName) ? "Chưa có" : member.MemberName)}\t" +
                                         $"{(member.TotalTasks > 0 ? member.TotalTasks.ToString() : "Chưa có")}\t" +
                                         $"{(member.TotalSubTasks > 0 ? member.TotalSubTasks.ToString() : "Chưa có")}\t" +
                                         $"{(member.CompletedSubTasks > 0 ? member.CompletedSubTasks.ToString() : "Chưa có")}\t" +
                                         $"{(member.ProgressPercentage >= 0 ? member.ProgressPercentage.ToString() : "Chưa có")}%");
            }
            reportBuilder.AppendLine();

            // Nhiệm vụ có hạn nộp sắp đến hoặc đã trễ
            reportBuilder.AppendLine("Nhiệm vụ có hạn nộp sắp đến hoặc đã trễ");
            reportBuilder.AppendLine("Tên nhiệm vụ\tHạn nộp\tTrạng thái\tSố ngày còn lại");

            foreach (var task in projectProgress.ProjectIssuesView.TasksWithDeadlineIssues)
            {
                reportBuilder.AppendLine($"{(string.IsNullOrEmpty(task.TaskName) ? "Chưa có" : task.TaskName)}\t" +
                                         $"{(task.EndDate != default ? task.EndDate.ToString("dd/MM/yyyy") : "Chưa có")}\t" +
                                         $"{(string.IsNullOrEmpty(task.UpdatedStatus) ? "Chưa có" : task.UpdatedStatus)}\t" +
                                         $"{(task.DaysRemaining >= 0 ? task.DaysRemaining.ToString() : "Chưa có")}");
            }
            reportBuilder.AppendLine();

            // Thành viên không cập nhật tiến độ định kỳ
            reportBuilder.AppendLine("Thành viên không cập nhật tiến độ định kỳ");
            reportBuilder.AppendLine("Tên thành viên\tEmail\tNhắc nhở");

            foreach (var member in projectProgress.ProjectIssuesView.MembersNotUpdatingProgress)
            {
                reportBuilder.AppendLine($"{(string.IsNullOrEmpty(member.FullName) ? "Chưa có" : member.FullName)}\t" +
                                         $"{(string.IsNullOrEmpty(member.Email) ? "Chưa có" : member.Email)}\tNhắc nhở");
            }
            reportBuilder.AppendLine();

            // Thành viên không nhận bất kỳ nhiệm vụ nào
            reportBuilder.AppendLine("Thành viên không nhận bất kỳ nhiệm vụ nào");
            reportBuilder.AppendLine("Tên thành viên\tEmail\tNhắc nhở");

            foreach (var member in projectProgress.ProjectIssuesView.MembersWithoutTasks)
            {
                reportBuilder.AppendLine($"{(string.IsNullOrEmpty(member.FullName) ? "Chưa có" : member.FullName)}\t" +
                                         $"{(string.IsNullOrEmpty(member.Email) ? "Chưa có" : member.Email)}\tNhắc nhở");
            }
            reportBuilder.AppendLine();

            // Kế hoạch tiến độ của nhóm
            reportBuilder.AppendLine("Kế hoạch tiến độ của nhóm");

            foreach (var phase in projectProgress.GetProjectPhases)
            {
                reportBuilder.AppendLine($"{(string.IsNullOrEmpty(phase.StageName) ? "Chưa có" : phase.StageName)}: Từ: " +
                                         $"{(phase.StartDate != default ? phase.StartDate.ToString("dd/MM/yyyy") : "Chưa có")} - Đến: " +
                                         $"{(phase.EndDate != default ? phase.EndDate.ToString("dd/MM/yyyy") : "Chưa có")}");
            }

            return reportBuilder.ToString();
        }





    }
}

