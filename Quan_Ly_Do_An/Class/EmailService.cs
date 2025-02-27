using MimeKit;
using MailKit.Net.Smtp;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Quan_Ly_Do_An.Data;
using Azure.Core;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Microsoft.EntityFrameworkCore;


namespace Quan_Ly_Do_An.Class
{
    public class EmailService/* : IEmailService*/
    {
        private readonly DatabaseDoAnContext _dbAnContext;
        private readonly SmtpSettings _smtpSettings;

        // Constructor để tiêm cả DatabaseDoAnContext và SmtpSettings
        public EmailService(DatabaseDoAnContext dbAnContext, IOptions<SmtpSettings> smtpSettings)
        {
            _dbAnContext = dbAnContext;
            _smtpSettings = smtpSettings.Value;
        }



        // Phương thức gửi email chung cho người dùng và giảng viên
        public async System.Threading.Tasks.Task SendEmailAsync(string toEmail, string subject, string body, string userNameEmail)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
            emailMessage.To.Add(new MailboxAddress(userNameEmail, toEmail)); // Người nhận email
            emailMessage.Subject = subject;


            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_smtpSettings.SmtpServer, _smtpSettings.SmtpPort, false);
                await client.AuthenticateAsync(_smtpSettings.SmtpUser, _smtpSettings.SmtpPass);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }

}
/*        public async System.Threading.Tasks.Task SendReminderEmailAsync()
        {
            var currentDate = DateTime.Now;

            // Truy vấn các báo cáo tiến độ chưa được nộp, kết hợp với nhiệm vụ và tính toán ngày nhắc nhở
            var query = from u in _dbAnContext.Users
                        join pt in _dbAnContext.ProjectTeams on u.UserId equals pt.UserId
                        join p in _dbAnContext.Projects on pt.ProjectId equals p.ProjectId
                        join cm in _dbAnContext.ClassMembers on p.ClassId equals cm.ClassId
                        join c in _dbAnContext.Classes on cm.ClassId equals c.ClassId
                        join pl in _dbAnContext.Users on p.ProjectLeaderId equals pl.UserId into projectLeaders
                        from pl in projectLeaders.DefaultIfEmpty()
                        join i in _dbAnContext.Users on c.InstructorId equals i.UserId into instructors
                        from i in instructors.DefaultIfEmpty()
                        join t in _dbAnContext.Tasks on p.ProjectId equals t.ProjectId into tasks // Join với bảng Tasks
                        from t in tasks.DefaultIfEmpty() // Lấy nhiệm vụ cho dự án
                        where t != null && // Đảm bảo có nhiệm vụ
                              t.StartDate.HasValue && // Nhiệm vụ phải có ngày bắt đầu
                              (currentDate - t.StartDate.Value).Days >= 2 // Nếu quá 2 ngày kể từ khi bắt đầu nhiệm vụ
                        group new { u, c, p, pl, i, t } by new
                        {
                            UserName = u.FullName,
                            UserEmail = u.Email, // Thêm email của người dùng
                            ClassName = c.ClassName,
                            ProjectName = p.ProjectName,
                            ProjectLeader = pl.FullName,
                            Instructor = i.FullName, // Thêm giảng viên
                            InstructorEmail = i.Email, // Thêm email của giảng viên
                            TaskStartDate = t.StartDate, // Thêm ngày bắt đầu nhiệm vụ
                            TaskEndDate = t.EndDate // Thêm ngày kết thúc nhiệm vụ
                        } into g
                        select new
                        {
                            UserName = g.Key.UserName,
                            UserEmail = g.Key.UserEmail, // Trả về email của người dùng
                            ClassName = g.Key.ClassName,
                            ProjectName = g.Key.ProjectName,
                            ProjectLeader = g.Key.ProjectLeader,
                            Instructor = g.Key.Instructor,
                            InstructorEmail = g.Key.InstructorEmail, // Trả về email giảng viên
                            TaskStartDate = g.Key.TaskStartDate, // Trả về ngày bắt đầu nhiệm vụ
                            TaskEndDate = g.Key.TaskEndDate // Trả về ngày kết thúc nhiệm vụ
                        };

            // Lấy danh sách các user chưa nộp báo cáo và chưa có nhiệm vụ
            var users = await query.ToListAsync(); // Chuyển query về danh sách đối tượng

            // Lọc các user chưa nộp báo cáo hoặc chưa có nhiệm vụ
            var validUsers = users.Where(user =>
         !_dbAnContext.ProgressReports.Any(pr => pr.UserEmail == user.Email) && // So sánh với Email
         !_dbAnContext.Tasks.Any(t => t.AssignedToUserEmail == user.Email) // So sánh với Email
     ).ToList();



            // Gửi email nhắc nhở cho những người dùng này
            foreach (var user in validUsers)
            {
                var subject = "Nhắc nhở về việc nộp báo cáo tiến độ";
                var body = $"Chào {user.UserName},<br/>" +
                           $"Bạn chưa nộp báo cáo tiến độ cho công việc cho dự án {user.ProjectName} thuộc lớp {user.ClassName}.<br/>" +
                           $"Trưởng nhóm là {user.ProjectLeader}.<br/>" +
                           $"Nếu bạn tiếp tục chậm trễ, chúng tôi sẽ báo cáo điều này cho giảng viên {user.Instructor}.<br/>" +
                           $"Vui lòng nộp báo cáo tiến độ càng sớm càng tốt.<br/>";

                // Gửi email nhắc nhở cho người dùng
                await SendEmailAsync(user.UserEmail, subject, body, "Hệ thống quản lý dự án");

                // Kiểm tra nếu quá 4 ngày hoặc hết hạn nhiệm vụ, gửi email cho giảng viên
                if ((currentDate - user.TaskStartDate.Value).Days >= 4 || (user.TaskEndDate.HasValue && currentDate > user.TaskEndDate.Value))
                {
                    subject = "Báo cáo tiến độ trễ - cần sự can thiệp";
                    body = $"Chào {user.Instructor},<br/>" +
                           $"Người dùng {user.UserName} trong lớp {user.ClassName} chưa nộp báo cáo tiến độ cho dự án {user.ProjectName}.<br/>" +
                           $"Trưởng nhóm là {user.ProjectLeader}.<br/>" +
                           $"Nhiệm vụ bắt đầu từ {user.TaskStartDate.Value.ToShortDateString()} và đã quá hạn (hoặc hết hạn).<br/>" +
                           $"Vui lòng xem xét và có hành động phù hợp.<br/>";

                    // Gửi email cho giảng viên
                    await SendEmailAsync(user.InstructorEmail, subject, body, "Hệ thống quản lý dự án");
                }
            }
        }*/