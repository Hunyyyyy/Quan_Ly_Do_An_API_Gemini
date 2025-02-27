using Microsoft.EntityFrameworkCore;
using Quan_Ly_Do_An.Class;
using Quan_Ly_Do_An.Data;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình dịch vụ SMTP từ appsettings.json
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddHttpClient<GeminiService>();
// Đăng ký dịch vụ email và AutoReminderEmailService vào DI
builder.Services.AddScoped<EmailService>(); // Đăng ký IEmailService là Scoped
/*builder.Services.AddHostedService<AutoReminderEmailService>(); // Đăng ký AutoReminderEmailService là Singleton
*/

// Cấu hình DbContext cho DatabaseDoAnContext
builder.Services.AddDbContext<DatabaseDoAnContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Đăng ký dịch vụ FileUploadService
builder.Services.AddScoped<FileUploadService>();

// Cấu hình session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký dịch vụ khác
builder.Services.AddLogging();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AccessToken>();

// Đăng ký Controllers và Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Cấu hình pipeline yêu cầu HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Đăng ký Session
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Index}/{id?}");

app.Run();
