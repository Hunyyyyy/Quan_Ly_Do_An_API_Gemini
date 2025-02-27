/*using Quan_Ly_Do_An.Controllers;

namespace Quan_Ly_Do_An.Class
{
    public class AutoReminderEmailService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public AutoReminderEmailService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Tạo scope mới để tiêm dịch vụ scoped (IEmailService)
                using (var scope = _serviceProvider.CreateScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>(); // Tiêm IEmailService từ scope
                    await emailService.SendReminderEmailAsync(); // Gọi phương thức gửi email
                }

                // Đợi 1 khoảng thời gian trước khi thực hiện lại (10 phút hoặc thời gian bạn muốn)
                await Task.Delay(TimeSpan.FromSeconds(10000), stoppingToken);
            }
        }
    }


}
*/