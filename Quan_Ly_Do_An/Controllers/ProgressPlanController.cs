using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quan_Ly_Do_An.Class;
using Quan_Ly_Do_An.Data;
using Quan_Ly_Do_An.Models;

namespace Quan_Ly_Do_An.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ProgressPlanController : Controller
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly Class.AccessToken _accessToken;
        private readonly DatabaseDoAnContext _dbAnContext;
        private readonly FileUploadService _fileUploadService;
        private readonly Class.EmailService _email;

        private int? userId;
        private int? classId;
        private int? projectId;
        private string? email;
        private string? userName;

        public ProgressPlanController(IHttpContextAccessor contextAccessor,
                                        Class.AccessToken accessToken,
                                        DatabaseDoAnContext dbAnContext,
                                        FileUploadService fileUploadService,
                                        EmailService emailService)
        {
            _contextAccessor = contextAccessor;
            _accessToken = accessToken;
            _dbAnContext = dbAnContext;
            _fileUploadService = fileUploadService;
            userId = _contextAccessor.HttpContext.Session.GetInt32("UserId");
            email = _contextAccessor.HttpContext.Session.GetString("Email");
            classId = _contextAccessor.HttpContext.Session.GetInt32("classId");
            projectId = _contextAccessor.HttpContext.Session.GetInt32("ProjectId");
            userName = _contextAccessor.HttpContext.Session.GetString("UserName");
            _email = emailService;
        }

        [HttpPost("SavePlan")]
        public async Task<IActionResult> SavePlan([FromBody] ProjectPlanModel plan)
        {
            if (plan == null || plan.Phases == null || !plan.Phases.Any())
            {
                return BadRequest("Dữ liệu kế hoạch không hợp lệ.");
            }
            var updateProject = await _dbAnContext.Projects.SingleOrDefaultAsync(t => t.ProjectId == projectId);
            if (updateProject == null)
            {
                return NotFound(new { message = "Dự án không tồn tại." });
            }
            updateProject.StartDate = DateOnly.FromDateTime(plan.StartDate).ToDateTime(TimeOnly.MinValue);
            updateProject.EndDate = DateOnly.FromDateTime(plan.EndDate).ToDateTime(TimeOnly.MinValue);
            // Lưu các giai đoạn vào bảng ProjectMilestone
            foreach (var phase in plan.Phases)
            {
                var projectMilestone = new ProjectMilestone
                {
                    ProjectId =int.Parse( plan.ProjectId),  // Kiểm tra kiểu dữ liệu
                    StageName = phase.StageName,
                    StartDate = DateOnly.FromDateTime(phase.StartDate),  // Chuyển DateTime thành DateOnly
                    EndDate = DateOnly.FromDateTime(phase.EndDate),
                   
                };

                _dbAnContext.ProjectMilestones.Add(projectMilestone);
            }

            try
            {
                await _dbAnContext.SaveChangesAsync();
               
                return Ok(new { message = "Kế hoạch đã được lưu thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi lưu kế hoạch: {ex.Message}" });
            }
        }

    }

}
