using Google.Apis.PeopleService.v1.Data;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MimeKit;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using OfficeOpenXml.Style.XmlAccess;
using Quan_Ly_Do_An.Class;
using Quan_Ly_Do_An.Data;
using Quan_Ly_Do_An.Models;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;



namespace Quan_Ly_Do_An.Controllers
{
    public class GroupsController : Controller
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

        public GroupsController(IHttpContextAccessor contextAccessor, Class.AccessToken accessToken, DatabaseDoAnContext dbAnContext, FileUploadService fileUploadService, EmailService emailService, GeminiService geminiService)
        {
            _contextAccessor = contextAccessor;
            //khởi tạo trong hàm khởi tạo
            _accessToken = accessToken;
            _dbAnContext = dbAnContext;
            _fileUploadService = fileUploadService;

            userId = _contextAccessor.HttpContext.Session.GetInt32("UserId");
            email = _contextAccessor.HttpContext.Session.GetString("Email");
            classId = _contextAccessor.HttpContext.Session.GetInt32("classId");
            projectId = _contextAccessor.HttpContext.Session.GetInt32("ProjectId");
            userName = _contextAccessor.HttpContext.Session.GetString("UserName");
            _email = emailService;
            _geminiService = geminiService;
        }

        public async Task<IActionResult> Index()
        {
            
            _contextAccessor.HttpContext.Session.Remove("task");
            _contextAccessor.HttpContext.Session.Remove("Tasks");
            _contextAccessor.HttpContext.Session.Remove("ListTaskRecived");
            _contextAccessor.HttpContext.Session.Remove("ProjectId");
            // Lấy tất cả các ClassId mà sinh viên tham gia từ bảng ClassMembers
            var classIds = _dbAnContext.ClassMembers
                .Where(cm => cm.UserId == userId)  // Lọc theo UserId (sinh viên)
                .Select(cm => cm.ClassId)  // Chỉ lấy ClassId của các lớp mà sinh viên tham gia
                .ToList();  // Lấy kết quả dưới dạng danh sách

            if (classIds.Any())  // Kiểm tra xem sinh viên có tham gia lớp nào không
            {

                // Lấy thông tin các lớp học từ bảng Classes tương ứng với ClassId của sinh viên
                var classInfos = await _dbAnContext.Classes
                    .Where(c => classIds.Contains(c.ClassId))  // Lọc theo các ClassId mà sinh viên tham gia
                    .Select(c => new ClassModel
                    {
                        ClassId = c.ClassId,
                        ClassName = c.ClassName,
                        SubjectCode = c.SubjectCode,
                        InstructorId = c.InstructorId,
                        InstructorFullName = _dbAnContext.Users
                            .Where(u => u.UserId == c.InstructorId)
                            .Select(u => u.FullName)
                            .FirstOrDefault()  // Lấy tên giảng viên
                    })
                    .ToListAsync();  // Lấy danh sách các lớp học
              
                // Trả về View với danh sách các lớp học mà sinh viên tham gia
                return View(classInfos);
            }
            TempData["Message"] = "Chưa có lớp nào được tạo!";
            return RedirectToAction("Index","Hone");
        }
        [HttpGet]
        public async Task<IActionResult> GroupView(string menuAction)
        {

            var classModel = _contextAccessor.HttpContext.Session.GetObject<ClassModel>("ClassModel");
            if (classModel == null)
            {
                return RedirectToAction("Index", "Home"); // Xử lý nếu session bị mất
            }
            try
            {
                var data = await ReloadGroupView(classModel);

                var c = ViewBag.Phases;
                if (c == null || c.Count == 0)  // Kiểm tra nếu không có giai đoạn nào
                {
                    ViewBag.dontCreateTask = true;
                }

                ViewBag.DefaultAction = string.IsNullOrEmpty(menuAction) ? "ListTaskRecived" : menuAction;
                var content = await GetContent(menuAction);
                return View(data); // Trả về view với dữ liệu
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lớp này giảng viên chưa chọn nhóm,không có dữ liệu!";
                return RedirectToAction("Index", "Groups");
            }
    
        }

        //Action truy vấn thông tin của trang Group
        [HttpPost]
        public  IActionResult Group(ClassModel model, string menuAction)
        {
            _contextAccessor.HttpContext.Session.SetObject("ClassModel", model);
            // Kiểm tra người dùng đã đăng nhập
            if (email == null)
            {
                return RedirectToAction("Logout", "Account");
            }          
            ViewBag.DefaultAction = string.IsNullOrEmpty(menuAction) ? "ListTask" : menuAction;
            return RedirectToAction("GroupView", new { menuAction });
        }


        private  List<ProjectPhaseViewModel>GetProjectPhases(int projectId)
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

        [HttpPost]
        public async Task<IActionResult> EditPhase(int MilestoneId, string StageName, DateTime StartDate, DateTime EndDate)
        {
            var classModel = _contextAccessor.HttpContext.Session.GetObject<ClassModel>("ClassModel");

            try
            {
                var phase = _dbAnContext.ProjectMilestones.Find(MilestoneId);
                if (phase == null)
                {
                    return Json(new { success = false, message = "Giai đoạn không tồn tại." });
                }

                phase.StageName = StageName;
                phase.StartDate = DateOnly.FromDateTime(StartDate);
                phase.EndDate = DateOnly.FromDateTime(EndDate);

                _dbAnContext.SaveChanges();
               

                // Trả về kết quả cho Ajax
                return Json(new { success = true, message = "Cập nhật giai đoạn thành công!" });
            }
            catch (Exception ex)
            {
               

                // Trả về lỗi cho Ajax
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật giai đoạn." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePhase(int id)
        {
            // Lấy thông tin lớp từ Session (nếu cần thiết)
            var classModel = _contextAccessor.HttpContext.Session.GetObject<ClassModel>("ClassModel");

            try
            {
                var phase = await _dbAnContext.ProjectMilestones.FindAsync(id);
                if (phase == null)
                {
                    return Json(new { success = false, message = "Giai đoạn không tồn tại." });
                }

                // Xóa giai đoạn
                _dbAnContext.ProjectMilestones.Remove(phase);
                await _dbAnContext.SaveChangesAsync();

                // Trả về thông báo thành công
                return Json(new { success = true, message = "Giai đoạn đã được xóa thành công." });
            }
            catch (Exception ex)
            {
              
                // Trả về thông báo lỗi
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xóa giai đoạn." });
            }
        }

        public static List<string> ConvertSubmissionTypes(string submissionTypes)
        {
            return submissionTypes != null ? submissionTypes.Split(',').ToList() : new List<string>();
        }

        //Action lưu nhiệm vụ
        [HttpPost]
        public async Task<IActionResult> CreateTaskFun(TaskModel model, int projectId)
        {
            if (email == null)
            {
                return View("Index", "Account");
            }

            if (ModelState.IsValid)
            {
                string attachmentPath = null; // Biến để lưu đường dẫn file đính kèm

                // Kiểm tra nếu có file đính kèm được tải lên
                if (model.Attachments != null && model.Attachments.Length > 0)
                {
                    try
                    {
                        // Sử dụng FileUploadService để lưu file và lấy đường dẫn
                        var fileUploadService = new FileUploadService();
                        attachmentPath = await fileUploadService.SaveFileAsync(model.Attachments);
                    }
                    catch (Exception ex)
                    {
                        TempData["Message"] = $"Lỗi khi tải lên file: {ex.Message}";
                        return View("Group", model); // Nếu có lỗi khi tải file, quay lại view Group
                    }
                }

                // Chuyển danh sách SubmissionTypes thành một chuỗi nếu có dữ liệu
                string submissionTypes = model.SubmissionTypes != null
                    ? string.Join(", ", model.SubmissionTypes)
                    : null;

                // Tạo mới một bản ghi Task
                var task = new Data.Task
                {
                    ProjectId = projectId,
                    TaskName = model.TaskName,
                    Description = model.Description,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Status = model.Status,
                    AssignedToUserId=model.AssignedToUserId,
                    SubmittedFileTypes = submissionTypes, // Lưu danh sách các loại file dưới dạng chuỗi
                    Attachment = attachmentPath, // Lưu đường dẫn file nếu có
                    AdditionalNotes = model.Notes,
                    PriorityLevel = model.Priority,
                };

                // Thêm bản ghi vào cơ sở dữ liệu
                _dbAnContext.Tasks.Add(task);
                await _dbAnContext.SaveChangesAsync();

                TempData["Message"] = "Tạo nhiệm vụ thành công!";

                return RedirectToAction("CreateTaskView", new { projectId = projectId });

            }

            TempData["Message"] = "Tạo nhiệm vụ thất bại!";
            return View("Group", model); // Nếu có lỗi, quay lại view Group với model hiện tại
        }

        //Giao diện của trang tạo nhiệm vụ
        [HttpGet]
        public async Task<IActionResult>CreateTaskView(int projectId)
        {
            if (email == null)
            {
                return View("Index", "Account");
            }
            var member = await(from pt in _dbAnContext.ProjectTeams
                               join u in _dbAnContext.Users on pt.UserId equals u.UserId
                               where pt.ProjectId == projectId
                               select new
                               {
                                   pt.UserId,
                                   AssignedUserName = u.FullName // Thay đổi theo tên cột trong bảng Users
                               }).ToListAsync();
            ViewBag.member = member;
            ViewBag.Phases = GetProjectPhases(projectId);
            ViewBag.ProjectId = projectId;
            return View();
        }
        public IActionResult CreateProgressPlan(int projectId)
        {
            if (email == null)
            {
                return View("Index", "Account");
            }
            ViewBag.ProjectId = projectId;
            var PName = _dbAnContext.Projects.FirstOrDefault(r=>r.ProjectId== projectId);
            ViewBag.PName = PName.ProjectName;
            ViewBag.PStartDate = PName.StartDate;
            ViewBag.PEndDate = PName.EndDate;
            return View();
        }
        //các action của ds nhiệm vụ
        [HttpPost]
        public async Task<List<TaskModel>> ListTask()
        {
            var projectId = _contextAccessor.HttpContext.Session.GetInt32("ProjectId");
            var tasks = new List<TaskModel>();

            // Kiểm tra xem người dùng có phải là trưởng nhóm không
            var leader = await _dbAnContext.Projects.SingleOrDefaultAsync(r => r.ProjectLeaderId == userId);

            if (leader != null)
            {
                // Nếu là trưởng nhóm, lấy tất cả nhiệm vụ trong dự án
                tasks = await _dbAnContext.Tasks
                    .Where(t => t.ProjectId == projectId)
                    .Select(t => new TaskModel
                    {
                        TaskId = t.TaskId,
                        ProjectId = t.ProjectId,
                        TaskName = t.TaskName,
                        Description = t.Description,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        Status = t.Status,
                        SubmissionTypes = ConvertSubmissionTypes(t.SubmittedFileTypes),
                        AttachmentPath = t.Attachment,
                        Notes = t.AdditionalNotes,
                        AssignedToUserId = t.AssignedToUserId,
                        Priority = t.PriorityLevel,
                        // Nếu AssignedToUserId không null, lấy thông tin người dùng
                        AssignedUserName = t.AssignedToUserId != null
                            ? _dbAnContext.Users
                                .Where(u => u.UserId == t.AssignedToUserId)
                                .Select(u => u.FullName)
                                .FirstOrDefault()
                            : "Chưa có người nhận" // Nếu không có người nhận, giá trị là null
                    })
                    .ToListAsync();
            }
            else
            {
                // Nếu không phải trưởng nhóm (tức là thành viên), chỉ lấy nhiệm vụ của người dùng
                tasks = await _dbAnContext.Tasks
                    .Where(t => t.ProjectId == projectId && t.AssignedToUserId == userId)
                    .Select(t => new TaskModel
                    {
                        TaskId = t.TaskId,
                        ProjectId = t.ProjectId,
                        TaskName = t.TaskName,
                        Description = t.Description,
                        StartDate = t.StartDate,
                        EndDate = t.EndDate,
                        Status = t.Status,
                        SubmissionTypes = ConvertSubmissionTypes(t.SubmittedFileTypes),
                        AttachmentPath = t.Attachment,
                        AssignedToUserId = t.AssignedToUserId,
                        Notes = t.AdditionalNotes,
                        Priority = t.PriorityLevel,
                        // Nếu AssignedToUserId không null, lấy thông tin người dùng
                        AssignedUserName = t.AssignedToUserId != null
                            ? _dbAnContext.Users
                                .Where(u => u.UserId == t.AssignedToUserId)
                                .Select(u => u.FullName)
                                .FirstOrDefault()
                            : "Chưa có người nhận" // Nếu không có người nhận, giá trị là null
                    })
                    .ToListAsync();
            }

            // Lấy danh sách thành viên trong nhóm
            var member = await (from pt in _dbAnContext.ProjectTeams
                                join u in _dbAnContext.Users on pt.UserId equals u.UserId
                                where pt.ProjectId == projectId
                                select new
                                {
                                    pt.UserId,
                                    AssignedUserName = u.FullName // Thay đổi theo tên cột trong bảng Users
                                }).ToListAsync();
            ViewBag.member = member;

            return tasks;
        }

        [HttpPost]
        public async Task<IActionResult> ApproveCancelTask(int taskId, int assignedUserId)
        {
            try
            {
                // Lấy nhiệm vụ từ database
                var task = _dbAnContext.Tasks.Find(taskId);
                if (task == null)
                {
                    return Json(new { success = false, message = "Nhiệm vụ không tồn tại." });
                }

                // Kiểm tra trạng thái nhiệm vụ
                if (task.Status != "Yêu cầu hủy nhiệm vụ")
                {
                    return Json(new { success = false, message = "Trạng thái nhiệm vụ không hợp lệ." });
                }

                // Cập nhật trạng thái nhiệm vụ và người được gán mới
                task.Status = "Đang chờ"; // Reset về trạng thái ban đầu
                task.AssignedToUserId = assignedUserId;
                _dbAnContext.SaveChanges();
                // Gửi email thông báo
                await _email.SendEmailAsync(
                email,
                    $"Trưởng nhóm đã chấp nhận yêu cầu hủy nhiệm vụ của bạn",
                    $"Trưởng nhóm đã chấp nhận yêu cầu hủy nhiệm vụ {task.TaskName} của bạn",
                    "Hệ thống quản lý dự án"
                );

                // Tạo thông báo và lưu vào database
                var notification = new Notification
                {
                    UserId = (int)userId, // Lấy UserId từ ClassModel
                    Message = $"Trưởng nhóm đã chấp nhận yêu cầu hủy nhiệm vụ {task.TaskName} của bạn",
                    DateSent = DateTime.Now,
                    IsRead = false,
                    ReceiverId = assignedUserId // Gửi thông báo đến trưởng nhóm
                };

                _dbAnContext.Notifications.Add(notification);
                await _dbAnContext.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> RejectCancelTask(int taskId)
        {
            try
            {
                // Lấy nhiệm vụ từ database
                var task = _dbAnContext.Tasks.Find(taskId);
                if (task == null)
                {
                    return Json(new { success = false, message = "Nhiệm vụ không tồn tại." });
                }

                // Kiểm tra trạng thái nhiệm vụ
                if (task.Status != "Yêu cầu hủy nhiệm vụ")
                {
                    return Json(new { success = false, message = "Trạng thái nhiệm vụ không hợp lệ." });
                }

                // Cập nhật trạng thái nhiệm vụ
                task.Status = "Đang chờ"; // Giữ nguyên trạng thái trước khi yêu cầu hủy
                _dbAnContext.SaveChanges();
                // Gửi email thông báo
                await _email.SendEmailAsync(
                email,
                    $"Trưởng nhóm đã từ chối yêu cầu hủy nhiệm vụ của bạn",
                    $"Trưởng nhóm đã chấp nhận yêu cầu hủy nhiệm vụ {task.TaskName} của bạn",
                    "Hệ thống quản lý dự án"
                );

                // Tạo thông báo và lưu vào database
                var notification = new Notification
                {
                    UserId = (int)userId, // Lấy UserId từ ClassModel
                    Message = $"Trưởng nhóm đã chấp nhận yêu cầu hủy nhiệm vụ {task.TaskName} của bạn",
                    DateSent = DateTime.Now,
                    IsRead = false,
                    ReceiverId =(int) task.AssignedToUserId // Gửi thông báo đến trưởng nhóm
                };

                _dbAnContext.Notifications.Add(notification);
                await _dbAnContext.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            // Lấy thông tin lớp từ Session
            var classModel = _contextAccessor.HttpContext.Session.GetObject<ClassModel>("ClassModel");

            if (classModel == null || taskId == 0)
            {
                TempData["Message"] = "Lỗi dữ liệu, không thể xóa nhiệm vụ.";
                return RedirectToAction("Index", "Home");
            }

            // Tìm Task cần xóa
            var task = await _dbAnContext.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == taskId);
            var subtask = await _dbAnContext.SubTasks.Where(t => t.TaskId == task.TaskId).ToListAsync();
            var report = await _dbAnContext.ProgressReports.Where(t => t.TaskId == task.TaskId).ToListAsync();

            // Tạo thông báo và thêm chi tiết nhiệm vụ phụ và báo cáo
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"Trưởng nhóm {userName} đã xóa nhiệm vụ {task.TaskName}.\nCác dữ liệu đã xóa gồm:");

            if (subtask.Any())
            {
                messageBuilder.AppendLine("- Các nhiệm vụ phụ:");
                foreach (var st in subtask)
                {
                    messageBuilder.AppendLine($"  + {st.SubTaskName}");
                }
            }

            if (report.Any())
            {
                messageBuilder.AppendLine("- Các báo cáo tiến độ:");
                foreach (var rp in report)
                {
                    messageBuilder.AppendLine($"  + {rp.WorkDescription}");
                }
            }

            // Tạo đối tượng Notification
            var notification = new Notification
            {
                UserId = (int)userId, // Lấy UserId từ ClassModel
                Message = messageBuilder.ToString(),
                DateSent = DateTime.Now,
                IsRead = false,
                ReceiverId = (int)task.AssignedToUserId // Gửi thông báo đến trưởng nhóm
            };

            _dbAnContext.Notifications.Add(notification);
            if (task == null)
            {
                TempData["Message"] = "Nhiệm vụ không tồn tại hoặc đang được thực hiện, xóa thất bại!";
                return RedirectToAction("GroupView", new { menuAction = "ListTask" });
            }

            // Lấy các báo cáo tiến độ và subtasks liên quan đến Task
            var progressReports = await _dbAnContext.ProgressReports
                .Where(pr => pr.TaskId == taskId)
                .ToListAsync();
            var subTasks = await _dbAnContext.SubTasks
                .Where(st => st.TaskId == taskId)
                .ToListAsync();

            // Xóa các thực thể khỏi cơ sở dữ liệu
            _dbAnContext.ProgressReports.RemoveRange(progressReports);
            _dbAnContext.SubTasks.RemoveRange(subTasks);
           
            _dbAnContext.Tasks.Remove(task);

            // Lưu thay đổi
            await _dbAnContext.SaveChangesAsync();

            // Lấy lại danh sách nhiệm vụ và render lại PartialView
            var tasks = await ListTask(); // Lấy lại danh sách nhiệm vụ
            var htmlContent = await RenderPartialViewToString("_TaskList", tasks);

            // Trả về JSON chứa HTML cập nhật
            return Json(new { success = true, htmlContent = htmlContent });
        }
        [HttpPost]
        public async Task<IActionResult> RecivedTask(int taskId, int projectId)
        {
            var task = await _dbAnContext.Tasks.SingleOrDefaultAsync(t => t.TaskId == taskId && t.ProjectId == projectId);
            string name = _contextAccessor.HttpContext.Session.GetString("UserName");
            if (task != null)
            {
              // Gán người dùng nhận nhiệm vụ
                task.Status = "Đang thực hiện"; // Cập nhật trạng thái

                await _dbAnContext.SaveChangesAsync();

                // Trả về thông báo thành công và nội dung cập nhật lại danh sách nhiệm vụ
                var tasks = await ListTask(); // Lấy lại danh sách nhiệm vụ mới
                return Json(new { success = true, message = "Nhiệm vụ đã được nhận thành công!", taskId = taskId,userName=name, tasksHtml = RenderPartialViewToString("_TaskList", tasks) });
            }
            else
            {
                return Json(new { success = false, message = "Không tìm thấy nhiệm vụ." });
            }
        }
        // kết thúc các action của ds nhiệm vụ
        //các action của ds nhiệm vụ đã nhận
        [HttpPost]
        public async Task<IActionResult> CancelTask(int TaskId, string Reason)
        {
            var classModel = _contextAccessor.HttpContext.Session.GetObject<ClassModel>("ClassModel");
            var email = _contextAccessor.HttpContext.Session.GetString("Email");
            var name = _contextAccessor.HttpContext.Session.GetString("UserName");
            var projectLeaderId = await _dbAnContext.Projects
                .Where(p => p.ProjectId ==projectId) // Thêm thông tin lấy từ session
                .Select(p => p.ProjectLeaderId)
                .FirstOrDefaultAsync();

            // Lấy task từ database
            var task = await _dbAnContext.Tasks.SingleOrDefaultAsync(t => t.TaskId == TaskId);

            // Kiểm tra nếu task tồn tại
            if (task != null)
            {
                // Cập nhật trạng thái nhiệm vụ
                task.Status = "Yêu cầu hủy nhiệm vụ"; // Cập nhật trạng thái thành trạng thái yêu cầu hủy

                // Lưu thay đổi vào database
                await _dbAnContext.SaveChangesAsync();

                // Gửi email thông báo
                await _email.SendEmailAsync(
                    email,
                    $"{name} muốn hủy nhiệm vụ này",
                    $"Với lý do: {Reason}",
                    "Hệ thống quản lý dự án"
                );

                // Tạo thông báo và lưu vào database
                var notification = new Notification
                {
                    UserId =(int) userId, // Lấy UserId từ ClassModel
                    Message = $"Người dùng {name} đã yêu cầu hủy nhiệm vụ. Lý do: {Reason}",
                    DateSent = DateTime.Now,
                    IsRead = false,
                    ReceiverId = projectLeaderId // Gửi thông báo đến trưởng nhóm
                };

                _dbAnContext.Notifications.Add(notification);
                await _dbAnContext.SaveChangesAsync();

                // Lấy lại danh sách nhiệm vụ sau khi cập nhật
                var tasks = await ListTask(); // Gọi lại phương thức để lấy danh sách nhiệm vụ mới
                var htmlContent = await RenderPartialViewToString("_TaskList", tasks);

                // Trả về JSON chứa nội dung HTML cập nhật và thông báo
                return Json(new
                {
                    success = true,
                    htmlContent = htmlContent,
                    message = "Yêu cầu hủy nhiệm vụ đã được gửi thành công!"
                });
            }
            else
            {
                // Nếu không tìm thấy task, trả về thông báo lỗi
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy nhiệm vụ!"
                });
            }
        }


        // kết thúc các action của ds nhiệm vụ đã nhận
        //các action của báo cáo
        //từ chối nhiệm vụ
        [HttpPost]
        public async Task<JsonResult> RefuseFinalReport(ProgressReportModel model, string WorkDescription)
        {
            if (userId != null)
            {
                model.UserId = userId;
            }

            // Log to debug the received values
            Console.WriteLine($"UserId: {model.UserId}, TaskId: {model.TaskId}, ProgressReportId: {model.ProgressReportId}");

            var progressReport = await _dbAnContext.ProgressReports
                .FirstOrDefaultAsync(r => r.TaskId == model.TaskId && r.ProgressReportId == model.ProgressReportId);
            var naem = model.ReporterName;
            if (progressReport == null)
            {
                // Log the error and return a failure message
                Console.WriteLine("Progress report not found!");
                return Json(new { success = false, message = "Không tìm thấy báo cáo để từ chối!" });
            }
            var userReport = await _dbAnContext.Users
                .SingleOrDefaultAsync(r => r.UserId == model.UserId);
            var taskName = await _dbAnContext.Tasks
               .SingleOrDefaultAsync(r => r.TaskId == model.TaskId && r.ProjectId == projectId);
            // Update report status and reason for refusal
            progressReport.Status = "Đã từ chối";
            progressReport.ReminderStatus = WorkDescription;
            string subject = $"Báo cáo của bạn trong nhiệm vụ '{taskName.TaskName}' đã bị từ chối";
            string body = $"{userName} đã từ chối báo cáo ngày {progressReport.ReportDate} của bạn với lý do {model.WorkDescription}";
            await _dbAnContext.SaveChangesAsync();
            await _email.SendEmailAsync(userReport.Email, subject, body, "Hệ thống quản lý dự án");
            var report = await GetListTaskFinalReportData(); // Gọi lại phương thức để lấy danh sách nhiệm vụ mới
            var htmlContent = await RenderPartialViewToString("_ListTaskFinalReport", report);
            return Json(new { success = true, htmlContent = htmlContent });
           // return Json(new { success = true, reportId = model.TaskId, message = $"Bạn đã từ chối báo cáo của {userReport.FullName} thành công!" });
        }
        [HttpPost]
        // Phê duyệt
        public async Task<JsonResult> AcceptFinalReport(ProgressReportModel model)
        {
            if (userId != null)
            {
                model.UserId = userId;
            }

            // Tìm báo cáo tiến độ
            var progressReport = await _dbAnContext.ProgressReports
                .FirstOrDefaultAsync(r => r.TaskId == model.TaskId && r.ProgressReportId == model.ProgressReportId);

            if (progressReport == null)
            {
                // Log lỗi và trả về thông báo thất bại
                Console.WriteLine("Progress report not found!");
                return Json(new { success = false, message = "Không tìm thấy báo cáo để phê duyệt!" });
            }

            // Cập nhật trạng thái báo cáo
            progressReport.Status = "Đã phê duyệt";

            // Cập nhật trạng thái nhiệm vụ
            var task = await _dbAnContext.Tasks
                .FirstOrDefaultAsync(t => t.TaskId == model.TaskId);

            if (task != null)
            {
                task.Status = "Hoàn thành"; // Đặt trạng thái nhiệm vụ là hoàn thành
            }
            else
            {
                return Json(new { success = false, message = "Không tìm thấy nhiệm vụ liên quan để cập nhật trạng thái!" });
            }

            // Gửi email thông báo
            var userReport = await _dbAnContext.Users
                .SingleOrDefaultAsync(r => r.UserId == model.UserId);

            var taskName = task?.TaskName;
            string subject = $"Báo cáo của bạn trong nhiệm vụ '{taskName}' đã được phê duyệt";
            string body = $"{userName} đã phê duyệt báo cáo ngày {progressReport.ReportDate} của bạn! \n***Lưu ý: Nếu bạn muốn sửa đổi báo cáo, hãy liên hệ với {userName} để được cấp quyền!";

            await _email.SendEmailAsync(userReport.Email, subject, body, "Hệ thống quản lý dự án");

            // Lưu thay đổi vào database
            await _dbAnContext.SaveChangesAsync();

            // Gọi lại phương thức để lấy danh sách nhiệm vụ mới
            var report = await GetListTaskFinalReportData();
            var htmlContent = await RenderPartialViewToString("_ListTaskFinalReport", report);

            return Json(new { success = true, htmlContent = htmlContent });
        }

        //kết thúc các action báo cáo
        private async Task<ProjectMilestoneAndProjectMemberView_GroupModel> ReloadGroupView(ClassModel model)
        {

            var classInfo = await _dbAnContext.ClassMembers
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ClassId == model.ClassId);

            if (classInfo == null)
            {
                throw new InvalidOperationException("User is not part of the class.");
            }

            var phases = await _dbAnContext.ProjectMilestones
                .Where(p => p.ProjectId == projectId)
                .Select(p => new
                {
                    p.StageName,
                    TotalDays = EF.Functions.DateDiffDay(p.StartDate, p.EndDate)
                })
                .ToListAsync();

            int totalDays = phases.Sum(p => p.TotalDays);
            var projectInfo = await _dbAnContext.ProjectTeams
                .Where(pm => pm.UserId == userId && pm.Project.ClassId == model.ClassId)
                .Select(pm => new ProjectMemberView_GroupModel
                {
                    ProjectId = pm.ProjectId,
                    ProjectName = pm.Project.ProjectName,
                    ClassId = pm.Project.ClassId,
                    ProjectLeaderId = pm.Project.ProjectLeaderId,
                    MemberUserId = pm.UserId,
                    GroupNumber = pm.GroupNumber,
                })
                .SingleOrDefaultAsync();

            var progressPlan = await _dbAnContext.ProjectMilestones
                .Where(r => r.ProjectId == projectInfo.ProjectId)
                .Select(r => new ProjectMilestoneModel
                {
                    MilestoneId = r.MilestoneId,
                    ProjectId = r.ProjectId,
                    Description = r.Description,
                    EndDate = r.EndDate,
                    StartDate = r.StartDate,
                    StageName = r.StageName,
                })
                .ToListAsync();

            var data = new ProjectMilestoneAndProjectMemberView_GroupModel
            {
                projectInfo = projectInfo,
                projectMilestone = progressPlan
            };
            // Chuẩn bị các thông tin cần thiết khác
            string leader = IsLeader(email, model.ClassId);
            _contextAccessor.HttpContext.Session.SetInt32("ProjectId", projectInfo.ProjectId);
            _contextAccessor.HttpContext.Session.SetInt32("classId", projectInfo.ClassId);
            _contextAccessor.HttpContext.Session.SetString("leader", leader);

            ViewBag.ClassName = model.ClassName;
            ViewBag.SubjectCode = model.SubjectCode;
            ViewBag.InstructorFullName = model.InstructorFullName;
            ViewBag.DefaultAction = "ListTask";
            ViewBag.Phases = GetProjectPhases(projectInfo.ProjectId);
          

            TempData["ProjectName"] = projectInfo.ProjectName;
            TempData["leader"] = leader;

            var tasks = await ListTask(); // Lấy danh sách nhiệm vụ
            ViewData["Tasks"] = tasks;
            return data;
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
        private async Task<List<TaskModel>> GetListTaskRecivedData()
        {
            // Lấy UserId và email của người dùng
            if (email == null || userId == null)
            {
                return null; // hoặc xử lý lỗi nếu người dùng chưa đăng nhập
            }
            var projectId = _contextAccessor.HttpContext.Session.GetInt32("ProjectId");
            var projectName = TempData["ProjectName"];

            // Truy vấn nhiệm vụ đã nhận của người dùng
            var tasks = await _dbAnContext.Tasks
               .Where(t => (t.AssignedToUserId == userId && (t.Status == "Đang thực hiện" || t.Status == "Hoàn thành")) && t.ProjectId == projectId)

                .ToListAsync();
            var taskModels = tasks.Select(t => new TaskModel
            {
                TaskId = t.TaskId,
                ProjectId = t.ProjectId,
                TaskName = t.TaskName,
                ProjectName=projectName as string,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                Status = t.Status,
                SubmissionTypes = ConvertSubmissionTypes(t.SubmittedFileTypes),
                AttachmentPath = t.Attachment,
                Notes = t.AdditionalNotes,
                Priority = t.PriorityLevel,
                // Nếu có người nhận, lấy tên người nhận
                AssignedUserName = t.AssignedToUserId != null
           ? _dbAnContext.Users
               .Where(u => u.UserId == t.AssignedToUserId)
               .Select(u => u.FullName)
               .FirstOrDefault()
           : "Chưa có người nhận"
            }).ToList();
            //_contextAccessor.HttpContext.Session.SetObject("ListTaskRecived", taskModels);
            // Trả về danh sách nhiệm vụ đã nhận
            return taskModels;
        }
        public async Task<List<ProgressReportModel>> GetListTaskReportData()
        {
            // Lấy TeamLeaderId từ session
            var teamLeaderId = _contextAccessor.HttpContext.Session.GetInt32("UserId");

            if (teamLeaderId == null)
            {
                return new List<ProgressReportModel>();
            }

            // Lấy ClassId của trưởng nhóm
            var classId = await (from cm in _dbAnContext.ClassMembers
                                 where cm.UserId == teamLeaderId.Value
                                 select cm.ClassId).FirstOrDefaultAsync();

            // Kiểm tra nếu classId không tồn tại
            if (classId == null)
            {
                return new List<ProgressReportModel>();
            }

            // Truy vấn LINQ
            // Lấy ClassId của trưởng nhóm dựa vào teamLeaderId
            var leaderClassId = await (from cm in _dbAnContext.ClassMembers
                                       join c in _dbAnContext.Classes on cm.ClassId equals c.ClassId
                                       where cm.UserId == teamLeaderId.Value
                                       select c.ClassId).FirstOrDefaultAsync();

            var reports = await (from pr in _dbAnContext.ProgressReports
                                 join t in _dbAnContext.Tasks on pr.TaskId equals t.TaskId
                                 join p in _dbAnContext.Projects on t.ProjectId equals p.ProjectId
                                 join cm in _dbAnContext.ClassMembers on pr.UserId equals cm.UserId
                                 join c in _dbAnContext.Classes on cm.ClassId equals c.ClassId
                                 where p.ProjectId == projectId
                                       && p.ProjectLeaderId == teamLeaderId.Value
                                       && c.ClassId == leaderClassId
                                       && pr.Status == "Đã Nộp" // Thêm điều kiện Status là "Đã Nộp"
                                 select new ProgressReportModel
                                 {
                                     TaskName = _dbAnContext.Tasks.SingleOrDefault(t => t.TaskId == pr.TaskId).TaskName,
                                     UserId = pr.UserId,
                                     WorkDescription = pr.WorkDescription,
                                     ReportDate = pr.ReportDate,
                                     Status = pr.Status,
                                     ReminderStatus = pr.ReminderStatus,
                                     ReporterName = _dbAnContext.Users.FirstOrDefault(u => u.UserId == pr.UserId).FullName
                                 }).ToListAsync();


            return reports;
        }

        public async Task<List<ProgressReportModel>> GetListTaskFinalReportData()
        {
            // Lấy TeamLeaderId từ session
            var teamLeaderId = _contextAccessor.HttpContext.Session.GetInt32("UserId");

            if (teamLeaderId == null)
            {
                return new List<ProgressReportModel>();
            }

            // Lấy ClassId của trưởng nhóm
            var classId = await (from cm in _dbAnContext.ClassMembers
                                 where cm.UserId == teamLeaderId.Value
                                 select cm.ClassId).FirstOrDefaultAsync();

            // Kiểm tra nếu classId không tồn tại
            if (classId == null)
            {
                return new List<ProgressReportModel>();
            }

            // Truy vấn LINQ
            // Lấy ClassId của trưởng nhóm dựa vào teamLeaderId
            var leaderClassId = await (from cm in _dbAnContext.ClassMembers
                                       join c in _dbAnContext.Classes on cm.ClassId equals c.ClassId
                                       where cm.UserId == teamLeaderId.Value
                                       select c.ClassId).FirstOrDefaultAsync();

            var reports = await (from pr in _dbAnContext.ProgressReports
                                 join t in _dbAnContext.Tasks on pr.TaskId equals t.TaskId
                                 join p in _dbAnContext.Projects on t.ProjectId equals p.ProjectId
                                 join cm in _dbAnContext.ClassMembers on pr.UserId equals cm.UserId
                                 join c in _dbAnContext.Classes on cm.ClassId equals c.ClassId
                                 where p.ProjectId == projectId
                                       && p.ProjectLeaderId == teamLeaderId.Value
                                       && c.ClassId == leaderClassId
                                       && (pr.Status == "Đang chờ phê duyệt"
                                            || pr.Status == "Đã phê duyệt"
                                            || pr.Status == "Đã từ chối") // Nhóm các điều kiện Status lại
                                 select new ProgressReportModel
                                 {
                                     TaskName = _dbAnContext.Tasks.SingleOrDefault(t => t.TaskId == pr.TaskId).TaskName,
                                     UserId = pr.UserId,
                                     WorkDescription = pr.WorkDescription,
                                     ReportDate = pr.ReportDate,
                                     Status = pr.Status,
                                     ReminderStatus = pr.ReminderStatus,
                                     TaskId = pr.TaskId,
                                     ProgressReportId = pr.ProgressReportId,
                                     ReporterName = _dbAnContext.Users.FirstOrDefault(u => u.UserId == pr.UserId).FullName
                                 }).ToListAsync();
            


            return reports;
        }

        private async Task<List<MemberTaskProgressViewModel>> GetMemberTaskProgress()
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

        private async Task<List<ProjectPhaseComparisonViewModel>> ComparePhaseProgress()
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

        public async Task<ProjectIssuesViewModel> CheckProjectIssues()
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

        public async Task<ProjectProgressViewModel> ViewProjectProgress()
        {
            try {

                var memberProgressTask =await  GetMemberTaskProgress();
                var phaseComparisonTask =await ComparePhaseProgress();
                var groupProgressTask =await GetGroupProgress();
                var groupProgressUserTask =await GroupProgress();
                var checkProjectIssues = await CheckProjectIssues();

               
                var model = new ProjectProgressViewModel
                {
                    MemberProgress = memberProgressTask,
                    PhaseComparison = phaseComparisonTask,
                    GroupProgress = groupProgressTask,
                    UserProgressView = groupProgressUserTask,
                    ProjectIssuesView = checkProjectIssues,
                };

                return model;

            }
            catch(Exception ex)
            {
                var m = ex.Message;
                return null;
            }
            
        }
        public async Task<List<SubTaskModel>> GetSubTasksWithTaskNameByProject()
        {
            var projectId = _contextAccessor.HttpContext.Session.GetInt32("ProjectId");
            var subTasksWithTaskName = await _dbAnContext.SubTasks
                .Include(st => st.Task) // Bao gồm liên kết với Tasks
                .Include(st => st.AssignedToUser) // Bao gồm liên kết với Users (người nhận nhiệm vụ)
                .Where(st => st.Task.ProjectId == projectId)
                .Select(st => new SubTaskModel
                {
                    SubTaskId = st.SubTaskId,
                    SubTaskName = st.SubTaskName,
                    Description = st.Description,
                    StartDate = st.StartDate,
                    EndDate = st.EndDate,
                    Status = st.Status,
                    AssignedToUserId = st.AssignedToUserId,
                    AssignedToUser = st.AssignedToUser == null ? null : new UserModel
                    {
                        UserId = st.AssignedToUser.UserId,
                        FullName = st.AssignedToUser.FullName, // Điều chỉnh theo cột trong bảng Users
                        Email = st.AssignedToUser.Email // Điều chỉnh theo cột trong bảng Users
                    },
                    // Chỉ ánh xạ đối tượng Task một cách đơn giản
                    TaskId = st.Task.TaskId, // Lấy TaskId trực tiếp
                    TaskName = st.Task.TaskName // Lấy TaskName trực tiếp
                })
                .ToListAsync();

            return subTasksWithTaskName;
        }



        public async Task<GroupProgressViewModel> GetGroupProgress()
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
        public async Task<List<UserProgressViewModel>> GroupProgress()
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

  


        public async Task<string> RenderPartialViewToString(string viewName, object model)
        {
            var controllerContext = this.ControllerContext;
            var actionContext = new ActionContext(controllerContext.HttpContext, controllerContext.RouteData, controllerContext.ActionDescriptor);

            // Create a temp view data
            var viewDataDictionary = new ViewDataDictionary<object>(this.ViewData)
            {
                Model = model
            };

            var tempDataDictionary = this.TempData;

            using (var sw = new StringWriter())
            {
                // Create a view engine to render the partial view
                var viewEngine = controllerContext.HttpContext.RequestServices.GetService<ICompositeViewEngine>();
                var viewResult = viewEngine.FindView(actionContext, viewName, false);

                if (viewResult.Success == false)
                {
                    throw new InvalidOperationException($"View '{viewName}' not found.");
                }

                var viewContext = new ViewContext(actionContext, viewResult.View, viewDataDictionary, tempDataDictionary, sw, new HtmlHelperOptions());
                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }
        public async Task< IActionResult >SendEmail()
        {
             var member = await(from pt in _dbAnContext.ProjectTeams
                                 join u in _dbAnContext.Users on pt.UserId equals u.UserId
                                 where pt.ProjectId == projectId
                                 select new
                                 {
                                     pt.UserId,
                                     AssignedUserName = u.FullName // Thay đổi theo tên cột trong bảng Users
                                 }).ToListAsync();
            ViewBag.member = member;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SendReminderEmail(int AssignedToUserId, string Subject, string Message)
        {
            // Lấy thông tin người dùng từ Session
            var userIdTeacher = _contextAccessor.HttpContext.Session.GetInt32("UserId");
            var userEmail = _contextAccessor.HttpContext.Session.GetString("Email");
            var userName = _contextAccessor.HttpContext.Session.GetString("UserName");
            var RecipientEmail = await _dbAnContext.Users.SingleOrDefaultAsync(t => t.UserId == AssignedToUserId);

            if (string.IsNullOrEmpty(Subject))
            {
                return BadRequest("Tiêu đề email không được để trống.");
            }

            if (string.IsNullOrEmpty(Message))
            {
                return BadRequest("Nội dung email không được để trống.");
            }

            try
            {
                await _email.SendEmailAsync(RecipientEmail.Email, Subject, Message, $"Người gửi {userName} ");
                // Thêm thông báo vào bảng Notifications
                var notification = new Notification
                {
                    UserId = (int)userIdTeacher,
                    Message = Message,
                    DateSent = DateTime.Now,
                    IsRead = false,
                    ReceiverId = AssignedToUserId,  // Lưu thông báo cho người nhận
                };

                _dbAnContext.Notifications.Add(notification);
                await _dbAnContext.SaveChangesAsync();
                TempData["Message"] = "Đã gửi thông báo qua Email thành công!";
                return RedirectToAction("SendEmail");
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết (nếu có hệ thống log)
                TempData["Message"] = "Đã gửi thông báo qua Email thất bại!";
                // Trả về mã lỗi với thông báo cụ thể
                return RedirectToAction("SendEmail");
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
                var question =await BuildQuestion(request.Answers);
                 var responses = await _geminiService.GenerateContentAsync(question);
                //var responses = question;

                return Json(new { success = true, response = responses });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, response = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }
        private async Task<AIForStudentModel> BuildProjectProgress()
        {
            
            int? ProjectId=_contextAccessor.HttpContext.Session.GetInt32("ProjectId");
            
            var classModel = _contextAccessor.HttpContext.Session.GetObject<ClassModel>("ClassModel");
            return new AIForStudentModel
            {
               Tasks= await ListTask(),
               TasksRecived= await GetListTaskRecivedData(),
               ProgressReport = await GetListTaskReportData(),
               TasksFinalReport= await GetListTaskFinalReportData(),
               ProjectProgressView = await ViewProjectProgress(),
               ProjectMilestoneAndProjectMemberView_Group=await ReloadGroupView(classModel),
               ProjectPhaseView = GetProjectPhases((int)ProjectId)

            };
        }
        private async Task<string> BuildQuestion(string userQuestion)
        {
            // Xây dựng báo cáo dự án
            var reportData = await BuildProjectReport();

            // Kiểm tra loại câu hỏi người dùng yêu cầu
            if (userQuestion.Equals("Analysis", StringComparison.OrdinalIgnoreCase))
            {
                return $"Tổng hợp dữ liệu dự án và các báo cáo thống kê:\n{reportData}";
            }
            else
            {
                // Nếu câu hỏi liên quan đến dự án, gửi dữ liệu và yêu cầu trả lời đúng trọng tâm
                return $"Dữ liệu dự án của tôi gồm các thông tin dưới đây. Bạn hãy trả lời câu hỏi về dự án này một cách rõ ràng và chính xác.Nếu câu hỏi liên quan đến các vấn đề về dữ liệu đã cung cấp thì hãy trả lời liên quan đến dữ liệu đó,Nếu không thì hãy bỏ qua dữ liệu đó.Chú ý: Nếu là các câu hỏi ngoài lề,ngoài phạn vi về xây dụng hệ thống,đồ án, hãy trả lời 'Câu hỏi của bạn không hợp lệ'.\n\n{reportData}\n\nCâu hỏi của người dùng: {userQuestion}";
            }
        }
        private async Task<string> BuildProjectReport()
        {
            var projectData = await BuildProjectProgress();
            var classModel = _contextAccessor.HttpContext.Session.GetObject<ClassModel>("ClassModel");
            string leader = _contextAccessor.HttpContext.Session.GetString("leaderName");

            // Lấy thông tin các nhiệm vụ phụ
            var subtasks = await GetSubTasksWithTaskNameByProject();

            // Dùng StringBuilder để xây dựng báo cáo
            var reportBuilder = new StringBuilder();

            // Lấy thông tin cơ bản của dự án
            var projectInfo = projectData.ProjectMilestoneAndProjectMemberView_Group.projectInfo;
            reportBuilder.AppendLine($"{classModel.ClassName} - Lớp: {classModel.SubjectCode}");
            reportBuilder.AppendLine($"Giảng Viên: {classModel.InstructorFullName}");
            reportBuilder.AppendLine($"Đề tài: {projectInfo.ProjectName} | Nhóm: {projectInfo.GroupNumber} | Trưởng nhóm: {leader}");
            reportBuilder.AppendLine();

            reportBuilder.AppendLine("Kế hoạch tiến độ");
            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Chú thích các giai đoạn:");

            // Lấy thông tin các giai đoạn
            foreach (var milestone in projectData.ProjectMilestoneAndProjectMemberView_Group.projectMilestone)
            {
                reportBuilder.AppendLine($" {milestone.StageName}: Từ: {milestone.StartDate:dd/MM/yyyy} - Đến: {milestone.EndDate:dd/MM/yyyy}");
            }

            reportBuilder.AppendLine();
            reportBuilder.AppendLine("Danh sách nhiệm vụ chính và nhiệm vụ phụ");

            // Lấy danh sách nhiệm vụ và nhiệm vụ phụ
            foreach (var task in projectData.Tasks)
            {
                reportBuilder.AppendLine($" {task.TaskName}");
                reportBuilder.AppendLine($" Bắt đầu: {task.StartDate:dd/MM/yyyy hh:mm:ss tt}");
                reportBuilder.AppendLine($" Kết thúc: {task.EndDate:dd/MM/yyyy hh:mm:ss tt}");
                reportBuilder.AppendLine($" Trạng thái: {task.Status}");
                reportBuilder.AppendLine($" Người thực hiện: {task.AssignedUserName}");
                reportBuilder.AppendLine($" Ưu tiên: {task.Priority}");
                reportBuilder.AppendLine(" Nhiệm vụ phụ:");

                // Thêm nhiệm vụ phụ của nhiệm vụ hiện tại
                var taskSubtasks = subtasks.Where(st => st.TaskId == task.TaskId).ToList();
                if (taskSubtasks.Any())
                {
                    foreach (var subtask in taskSubtasks)
                    {
                        reportBuilder.AppendLine($"   - {subtask.SubTaskName}: {subtask.Description}");
                        reportBuilder.AppendLine($"     Bắt đầu: {subtask.StartDate:dd/MM/yyyy hh:mm:ss tt}");
                        reportBuilder.AppendLine($"     Kết thúc: {subtask.EndDate:dd/MM/yyyy hh:mm:ss tt}");
                        reportBuilder.AppendLine($"     Người nhận: {subtask.AssignedToUser?.FullName ?? "Chưa gán"}");
                        reportBuilder.AppendLine($"     Trạng thái: {subtask.Status}");
                    }
                }
                else
                {
                    reportBuilder.AppendLine($"   Không có nhiệm vụ phụ nào.");
                }

                reportBuilder.AppendLine();
            }

            // Các phần khác của báo cáo không thay đổi
            reportBuilder.AppendLine("Danh sách nhiệm vụ đã nhận");

            foreach (var taskReceived in projectData.TasksRecived)
            {
                reportBuilder.AppendLine($" {taskReceived.TaskName}");
                reportBuilder.AppendLine($" Bắt đầu: {taskReceived.StartDate:dd/MM/yyyy hh:mm:ss tt}");
                reportBuilder.AppendLine($" Kết thúc: {taskReceived.EndDate:dd/MM/yyyy hh:mm:ss tt}");
                reportBuilder.AppendLine($" Trạng thái: {taskReceived.Status}");
                reportBuilder.AppendLine($" Ưu tiên: {taskReceived.Priority}");
                reportBuilder.AppendLine();
            }

            reportBuilder.AppendLine("Bảng số liệu tổng thể");
            reportBuilder.AppendLine("Chỉ số\tGiá trị");
            reportBuilder.AppendLine($"Tổng số nhiệm vụ đã tạo\t{projectData.ProjectProgressView.GroupProgress.TotalTasksCreated}");
            reportBuilder.AppendLine($"Tổng số nhiệm vụ đã nhận\t{projectData.ProjectProgressView.GroupProgress.TotalTasksAssigned}");
            reportBuilder.AppendLine($"Tổng số công việc phụ đã tạo\t{projectData.ProjectProgressView.GroupProgress.TotalSubTasksCreated}");
            reportBuilder.AppendLine($"Tổng số báo cáo tiến độ đã nộp\t{projectData.ProjectProgressView.GroupProgress.TotalReportsSubmitted}");
            reportBuilder.AppendLine($"Tổng số báo cáo hoàn thiện đã nộp\t{projectData.ProjectProgressView.GroupProgress.TotalReportsCompleted}");
            reportBuilder.AppendLine($"Tổng số báo cáo bị từ chối\t{projectData.ProjectProgressView.GroupProgress.TotalReportsFail}");
            reportBuilder.AppendLine($"Tỷ lệ hoàn thành\t{projectData.ProjectProgressView.GroupProgress.CompletionPercentage}%");
            reportBuilder.AppendLine();

            reportBuilder.AppendLine("So sánh tiến độ giai đoạn");
            reportBuilder.AppendLine("Tên giai đoạn\tSố ngày kế hoạch\tTiến độ thực tế (%)\tSố ngày đã trôi qua\tTổng số công việc\tSố công việc đã hoàn thành\tTrạng thái");

            foreach (var phase in projectData.ProjectProgressView.PhaseComparison)
            {
                reportBuilder.AppendLine($"{phase.StageName}\t{phase.PlannedDays}\t{phase.ActualProgress}%\t{phase.DaysElapsed}\t{phase.TotalSubTasks}\t{phase.CompletedSubTasks}\t{phase.Status.ToUpper()}");
            }

            reportBuilder.AppendLine();

            reportBuilder.AppendLine("Tiến độ công việc của các thành viên");
            reportBuilder.AppendLine("Tên thành viên\tSố nhiệm vụ\tTổng số công việc phụ\tCông việc phụ hoàn thành\tPhần trăm tiến độ");

            foreach (var member in projectData.ProjectProgressView.MemberProgress)
            {
                reportBuilder.AppendLine($"{member.MemberName}\t{member.TotalTasks}\t{member.TotalSubTasks}\t{member.CompletedSubTasks}\t{member.ProgressPercentage}%");
            }

            return reportBuilder.ToString();
        }


        // Tạo câu hỏi dựa trên câu trả lời và tiến độ
        //private string BuildQuestion(string answers, ProjectProgressViewModel projectProgress)
        //{
        //    var reportData = GenerateReport(projectProgress);
        //    return answers.Equals("Analysis", StringComparison.OrdinalIgnoreCase)
        //        ? "xây dựng một báo cáo chi tiết, hoàn chỉnh và chuyên nghiệp với dữ liệu sau.\n" + reportData
        //        : answers + " dựa vào dữ liệu sau *chú ý:trả lời trực quan và ngắn gọn nhất,NẾU CÂU HỎI KHÔNG LIÊN QUAN ĐẾN DỮ LIỆU HÃY TRẢ LỜI 'CÂU HỎI CỦA BẠN KHÔNG HỢP LỆ' \n" + reportData;
        //}
        [HttpPost]
        public IActionResult UpdateTask(TaskModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Truy vấn nhiệm vụ cần cập nhật từ cơ sở dữ liệu
                    var taskToUpdate = _dbAnContext.Tasks.FirstOrDefault(t => t.TaskId == model.TaskId);

                    if (taskToUpdate != null)
                    {
                        // Cập nhật các thông tin của nhiệm vụ
                        taskToUpdate.TaskName = model.TaskName;
                        taskToUpdate.Description = model.Description;
                        taskToUpdate.StartDate = model.StartDate;
                        taskToUpdate.EndDate = model.EndDate;
                        taskToUpdate.Status = model.Status;
                        taskToUpdate.AssignedToUserId = model.AssignedToUserId;
                        taskToUpdate.PriorityLevel = model.Priority;
                        taskToUpdate.AdditionalNotes = model.Notes;
                        taskToUpdate.Attachment = model.AttachmentPath;

                        // Lưu các thay đổi vào cơ sở dữ liệu
                        _dbAnContext.Update(taskToUpdate);
                        _dbAnContext.SaveChanges();

                        // Chuyển hướng về trang danh sách nhiệm vụ
                        TempData["Message"] = "Cập nhật nhiệm vụ thành công!";
                        return RedirectToAction("GroupView");
                    }
                    else
                    {
                        TempData["Message"] = "Nhiệm vụ không tồn tại!";
                        return RedirectToAction("GroupView");
                    }
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi nếu có
                    TempData["Message"] = "Có lỗi khi cập nhật nhiệm vụ: " + ex.Message;
                    return RedirectToAction("GroupView");
                }
            }
            else
            {
                // Nếu ModelState không hợp lệ
                TempData["Message"] = "Dữ liệu nhập vào không hợp lệ!";
                return RedirectToAction("GroupView");
            }
        }


        [HttpGet]
        public async Task<IActionResult>GetContent(string menuAction)
        {
            switch (menuAction)
            {
                case "ListTask":
                    var tasks = await ListTask();
                   
                    return PartialView("_TaskList",tasks);
                case "ListTaskRecived":
                    // Lấy danh sách nhiệm vụ đã nhận từ cơ sở dữ liệu
                    var tasksRecived = await GetListTaskRecivedData();
                        return PartialView("_ListTaskRecived", tasksRecived);
                case "Report":
                    // Lấy danh sách nhiệm vụ đã nhận từ cơ sở dữ liệuReport
                    var tasksReport = await GetListTaskReportData();
                    return PartialView("_ListTaskReport", tasksReport);
                case "FinalReport":
                    // Lấy danh sách nhiệm vụ đã nhận từ cơ sở dữ liệuReport
                    var tasksFinalReport = await GetListTaskFinalReportData();
                    return PartialView("_ListTaskFinalReport", tasksFinalReport);
                case "Progress":
                    // Lấy danh sách nhiệm vụ đã nhận từ cơ sở dữ liệuReport
                    var Progress = await ViewProjectProgress();
                    return PartialView("_Progress", Progress);
                default:
                    return PartialView("_DefaultContent");  // Mặc định hiển thị nếu không có action nào
            }
        }
        public string IsLeader(string email, int classId)
            {
                // Tìm người dùng theo email
                var user = _dbAnContext.Users.FirstOrDefault(u => u.Email == email);
                if (user == null)
                {
                    return "false"; // Nếu không tìm thấy người dùng
                }

            // Kiểm tra xem người dùng có phải là trưởng nhóm trong lớp và dự án nào không
            var project = (from c in _dbAnContext.Classes
                                 join p in _dbAnContext.Projects on c.ClassId equals p.ClassId
                                 join pt in _dbAnContext.ProjectTeams on p.ProjectId equals pt.ProjectId
                                 where c.ClassId == classId && p.ProjectLeaderId == user.UserId
                                 select p).Any();


            if (project)
                {

                _contextAccessor.HttpContext.Session.SetString("leaderName", userName);
                        return "IsLeader"; // Trưởng nhóm
                    
                }

                return "Member"; // Nếu không phải trưởng nhóm thì chỉ là thành viên
            }
        // Action để cập nhật thông tin nhiệm vụ
   
}

}
