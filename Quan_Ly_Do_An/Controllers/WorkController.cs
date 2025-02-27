using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Quan_Ly_Do_An.Class;
using Quan_Ly_Do_An.Data;
using Quan_Ly_Do_An.Models;
using System.Globalization;
using System.Threading.Tasks;

namespace Quan_Ly_Do_An.Controllers
{
    public class WorkController : Controller
    {
        private readonly IHttpContextAccessor _contextAccessor;
        //Khai báo AccessToken (lớp)
        private readonly Class.AccessToken _accessToken;
        private readonly DatabaseDoAnContext _dbAnContext;
        private readonly FileUploadService _fileUploadService;
        private int? userId;
        private int? classId;
        private string? email;
        private string? userName;
        /*private readonly EmailService _emailService;*/
        public WorkController(IHttpContextAccessor contextAccessor, Class.AccessToken accessToken, DatabaseDoAnContext dbAnContext, FileUploadService fileUploadService/*, EmailService emailService*/)
        {
            _contextAccessor = contextAccessor;
            /* _emailService = emailService;*/
            //khởi tạo trong hàm khởi tạo
            _accessToken = accessToken;
            _dbAnContext = dbAnContext;
            _fileUploadService = fileUploadService;
            userId = _contextAccessor.HttpContext.Session.GetInt32("UserId");
            email = _contextAccessor.HttpContext.Session.GetString("Email");
            classId = _contextAccessor.HttpContext.Session.GetInt32("classId");
            userName = _contextAccessor.HttpContext.Session.GetString("UserName");
        }

        [HttpPost]
        public async Task<IActionResult> GetDataIndex(TaskModel model)
        {
            try
            {
                // Cập nhật task từ model
                var task = new TaskModel
                {
                    TaskId = model.TaskId,
                    TaskName = model.TaskName,
                    Description = model.Description,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Status = model.Status,
                    Priority = model.Priority,
                    SubmissionTypes = model.SubmissionTypes,
                    Notes = model.Notes,
                    AttachmentPath = model.AttachmentPath,
                    ProjectName = model.ProjectName
                };

                // Lưu task vào session
                _contextAccessor.HttpContext.Session.SetObject("task", task);

                // Lấy dữ liệu chi tiết
                var data = await GetTaskDetails();
                _contextAccessor.HttpContext.Session.SetObject("TaskData", data);

                // Tính toán tiến độ
                ViewData["ProgressPercentage"] = await CalculateTaskProgress(task.TaskId);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
            }

            // Chuyển hướng về Index
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Index()
        {
            // Lấy dữ liệu từ session
            var data = _contextAccessor.HttpContext.Session.GetObject<Task_And_ProgressReportModel>("TaskData");

            if (data == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dữ liệu công việc.";
                return RedirectToAction("Index", "Account");
            }

            // Lấy phần trăm tiến độ từ TempData (nếu có)
            if (TempData.ContainsKey("ProgressPercentage"))
            {
                ViewData["ProgressPercentage"] = TempData["ProgressPercentage"];
            }

            // Hiển thị thông báo nếu có
            if (TempData.ContainsKey("SuccessMessage"))
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }
            if (TempData.ContainsKey("ErrorMessage"))
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }

            return View(data);
        }



        // Helper Method to Calculate Progress
        private async Task<int> CalculateTaskProgress(int taskId)
        {
            // Lấy tổng số SubTask và số SubTask hoàn thành
            var totalSubTasks = await _dbAnContext.SubTasks.CountAsync(st => st.TaskId == taskId);
            var completedSubTasks = await _dbAnContext.SubTasks.CountAsync(st => st.TaskId == taskId && st.Status == "Hoàn thành");

            // Tính toán tỷ lệ phần trăm tiến độ
            return totalSubTasks == 0 ? 0 : (int)((double)completedSubTasks / totalSubTasks * 100);
        }

        [HttpGet]
        public async Task<IActionResult> GetTaskProgress(int taskId)
        {
            int progressPercentage = await CalculateTaskProgress(taskId);
            return Json(new { progressPercentage });
        }



        public async Task<Task_And_ProgressReportModel> GetTaskDetails()
        {
           var projectId = _contextAccessor.HttpContext.Session.GetInt32("ProjectId");
            var task = _contextAccessor.HttpContext.Session.GetObject<TaskModel>("task");
            if (task == null) throw new Exception("Task data not found in session.");

            var listReport = await GetProgressReportsAsync(task.TaskId,(int) userId);
            var subTaskList = await GetSubTasksAsync(task.TaskId,(int) userId);

            var dismiss = listReport.SingleOrDefault(r => r.Status == "Đang chờ phê duyệt" || r.Status == "Đã phê duyệt");
            if (dismiss != null)
            {
                TempData["Dismiss"] = "true";
            }
            return new Task_And_ProgressReportModel
            {
                Task = task,
                Report = listReport,
                SubTask = subTaskList,
 
            };
        }

        private async Task<List<ProgressReportModel>> GetProgressReportsAsync(int taskId, int userId)
        {
            return await _dbAnContext.ProgressReports
                .Where(r => r.TaskId == taskId && r.UserId == userId)
                .Select(r => new ProgressReportModel
                {
                    AttachedFilePath = r.AttachedFile,
                    ReminderStatus = r.ReminderStatus,
                    ReportDate = r.ReportDate,
                    Status = r.Status,
                    WorkDescription = r.WorkDescription,
                }).ToListAsync();
        }

        private async Task<List<SubTaskModel>> GetSubTasksAsync(int taskId, int userId)
        {
            return await _dbAnContext.SubTasks
                .Where(r => r.TaskId == taskId && r.AssignedToUserId == userId)
                .Select(r => new SubTaskModel
                {
                    AssignedToUserId = r.AssignedToUserId,
                    Description = r.Description,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                    Status = r.Status,
                    SubTaskName = r.SubTaskName,
                    SubTaskId = r.SubTaskId,
                    TaskId = r.TaskId,
                }).ToListAsync();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProgressReport(ProgressReportModel model)
        {
            // Lấy TaskModel từ session
            var task = _contextAccessor.HttpContext.Session.GetObject<TaskModel>("task");
            
            if (task == null)
            {
                return NotFound("Task not found in session.");
            }

            if (userId != null)
            {
                model.UserId = userId;
            }

            // Cập nhật trạng thái báo cáo
            model.Status = "Đã Nộp";
            model.ReportDate = DateTime.Now;
            
            // Kiểm tra và xử lý file đính kèm
            string attachmentPath = null; // Biến để lưu đường dẫn file đính kèm

            // Kiểm tra nếu có file đính kèm được tải lên
            if (model.AttachedFile != null && model.AttachedFile.Length > 0)
            {
                try
                {
                    // Sử dụng FileUploadService để lưu file và lấy đường dẫn
                    var fileUploadService = new FileUploadService();
                    attachmentPath = await fileUploadService.SaveFileAsync(model.AttachedFile);
                    
                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Lỗi khi tải lên file: {ex.Message}";
                    return RedirectToAction("Index"); // Nếu có lỗi khi tải file, quay lại view Group
                }
            }
            else
            {
                TempData["Message"] = $"Bạn đã nộp báo cáo cho ngày {DateTime.Now} thành công";

                
            }

            // Gán TaskId từ TaskModel vào báo cáo


            // Chuyển đổi ProgressReportModel thành ProgressReport (nếu model khác loại)
            var progressReport = new ProgressReport
            {
                TaskId = model.TaskId,
                UserId = (int)model.UserId,
                ReportDate = model.ReportDate,
                WorkDescription = model.WorkDescription,
                Status = model.Status,
                ReminderStatus = model.ReminderStatus,
                AttachedFile = attachmentPath // Lưu đường dẫn file vào cơ sở dữ liệu
            };

            // Thêm bản ghi vào cơ sở dữ liệu
            _dbAnContext.ProgressReports.Add(progressReport);
            await _dbAnContext.SaveChangesAsync();
            // Cập nhật dữ liệu trong session
            var data = await GetTaskDetails();
            _contextAccessor.HttpContext.Session.SetObject("TaskData", data);
            // Hiển thị thông báo thành công
            TempData["Message"] = $"Bạn đã nộp báo cáo cho ngày {DateTime.Now} thành công";

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> CreateFinalFile(ProgressReportModel model)
        {
            // Lấy TaskModel từ session
            var task = _contextAccessor.HttpContext.Session.GetObject<TaskModel>("task");
      
            if (task == null)
            {
                return NotFound("Task not found in session.");
            }

            if (userId != null)
            {
                model.UserId = userId;
            }

            // Cập nhật trạng thái báo cáo
            model.Status = "Đang chờ phê duyệt";
           
            model.ReportDate = DateTime.Now;

            // Kiểm tra và xử lý file đính kèm
            string attachmentPath = null; // Biến để lưu đường dẫn file đính kèm

            // Kiểm tra nếu có file đính kèm được tải lên
            if (model.AttachedFile != null && model.AttachedFile.Length > 0)
            {
                try
                {
                    // Sử dụng FileUploadService để lưu file và lấy đường dẫn
                    var fileUploadService = new FileUploadService();
                    attachmentPath = await fileUploadService.SaveFileAsync(model.AttachedFile);

                }
                catch (Exception ex)
                {
                    TempData["Message"] = $"Lỗi khi tải lên file: {ex.Message}";
                    return RedirectToAction("Index"); // Nếu có lỗi khi tải file, quay lại view Group
                }
            }


            // Gán TaskId từ TaskModel vào báo cáo


            // Chuyển đổi ProgressReportModel thành ProgressReport (nếu model khác loại)
            var progressReport = new ProgressReport
            {
                TaskId = model.TaskId,
                UserId = (int)model.UserId,
                ReportDate = model.ReportDate,
                WorkDescription = model.WorkDescription,
                Status = model.Status,
                ReminderStatus = model.ReminderStatus,
                AttachedFile = attachmentPath // Lưu đường dẫn file vào cơ sở dữ liệu
            };

            // Thêm bản ghi vào cơ sở dữ liệu
            _dbAnContext.ProgressReports.Add(progressReport);
            await _dbAnContext.SaveChangesAsync();
            var data = await GetTaskDetails();
            _contextAccessor.HttpContext.Session.SetObject("TaskData", data);
            // Hiển thị thông báo thành công
            TempData["Message"] = $"Bạn đã nộp báo cáo hoàn thiện vào ngày {DateTime.Now} thành công";
            TempData["Dismiss"] = TempData["Dismiss"];
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewSubTask(SubTaskModel model)
        {
            try
            {
                var task = _contextAccessor.HttpContext.Session.GetObject<TaskModel>("task");
                if (task == null)
                {
                    TempData["ErrorMessage"] = "Dữ liệu công việc không hợp lệ.";
                    return RedirectToAction("Index");
                }

                var subTask = new SubTask
                {
                    AssignedToUserId = userId ?? 0,
                    Description = model.Description,
                    StartDate = DateTime.Now,
                    EndDate = model.EndDate,
                    Status = "Đang chờ",
                    SubTaskName = model.SubTaskName,
                    TaskId = model.TaskId,
                };

                _dbAnContext.SubTasks.Add(subTask);
                await _dbAnContext.SaveChangesAsync();

                // Cập nhật dữ liệu trong session
                var data = await GetTaskDetails();
                _contextAccessor.HttpContext.Session.SetObject("TaskData", data);

                // Lưu thông báo thành công
                TempData["Message"] = "Tạo công việc phụ thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            // Chuyển hướng về action Index (GET)
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> UpdateSubTask(int subId, string status, string type, string subname, SubTaskModel model)
        {
            try
            {
                var task = _contextAccessor.HttpContext.Session.GetObject<TaskModel>("task");
                if (task == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin công việc trong session!" });
                }

                if (type.Equals("update")) // Xử lý cập nhật chi tiết SubTask
                {
                    var subTask = await _dbAnContext.SubTasks.FindAsync(model.SubTaskId);
                    if (subTask == null)
                    {
                        return Json(new { success = false, message = "Công việc phụ không tồn tại!" });
                    }

                    subTask.SubTaskName = model.SubTaskName;
                    subTask.Description = model.Description;
                    subTask.EndDate = model.EndDate;

                    await _dbAnContext.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = $"Bạn vừa cập nhật công việc phụ '{model.SubTaskName}' thành công!",
                        updatedSubTask = new
                        {
                            SubTaskId = model.SubTaskId,
                            SubTaskName = model.SubTaskName,
                            Description = model.Description,
                            EndDate = string.Format("{0:dd/MM/yyyy}", model.EndDate)

                }
            });
                }
                else if (subId > 0 && !string.IsNullOrEmpty(type)) // Xử lý cập nhật trạng thái SubTask
                {
                    var statusMapping = new Dictionary<string, string>
            {
                { "start", "Đang thực hiện" },
                { "stop", "Tạm hoãn" },
                { "continue", "Đang thực hiện" },
                { "submit", "Hoàn thành" },
                { "edit", "Đang chỉnh sửa" }
            };

                    if (!statusMapping.TryGetValue(type, out var newStatus))
                    {
                        return Json(new { success = false, message = "Loại hành động không hợp lệ." });
                    }

                    var subTask = await _dbAnContext.SubTasks.FindAsync(subId);
                    if (subTask == null)
                    {
                        return Json(new { success = false, message = "Công việc phụ không tồn tại!" });
                    }

                    subTask.Status = newStatus;

                    await _dbAnContext.SaveChangesAsync();

                    //    TempData["DataTask"] = await GetTaskDetails();
                    TempData["DataTask"] = JsonConvert.SerializeObject(await GetTaskDetails());
                    var progressPercentage = await CalculateTaskProgress(task.TaskId);
                    return Json(new
                    {
                        success = true,
                        message = type switch
                        {
                            "start" => $"Xác nhận thực hiện công việc phụ '{subname}' thành công!",
                            "stop" => $"Xác nhận tạm hoãn công việc phụ '{subname}' thành công!",
                            "continue" => $"Xác nhận tiếp tục công việc phụ '{subname}' thành công!",
                            "submit" => $"Xác nhận hoàn thành công việc phụ '{subname}'!",
                            _ => $"Bạn đang chỉnh sửa công việc phụ '{subname}'!"
                        },
                        dataTask = TempData["DataTask"],// Gửi dữ liệu công việc về client
                        progress = progressPercentage // Gửi dữ liệu công việc về client
                    });

                }
                else
                {
                    return Json(new { success = false, message = "Yêu cầu không hợp lệ!" });
                }
            }
            catch (Exception ex)
            {
       
                return Json(new { success = false, message = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DateleSubTask(int SubId)
        {
            if (SubId <= 0)
            {
                return BadRequest("ID không hợp lệ.");
            }

            // Tìm SubTask theo ID
            var subTask = await _dbAnContext.SubTasks.FindAsync(SubId);

            if (subTask == null)
            {
                return NotFound("Không tìm thấy công việc phụ cần xóa.");
            }

            // Xóa SubTask
            _dbAnContext.SubTasks.Remove(subTask);
            await _dbAnContext.SaveChangesAsync();

            // Tạo thông báo sau khi xóa thành công
            ViewBag.Message = $"Đã xóa công việc phụ '{subTask.SubTaskName}' thành công!";

            // Lấy dữ liệu cập nhật
            var data = await GetTaskDetails();

            // Trả về View với dữ liệu đã cập nhật
            return View("Index", data);
        }


 

    }

}
