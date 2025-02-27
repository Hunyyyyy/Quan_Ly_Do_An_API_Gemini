using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Quan_Ly_Do_An.Class;
using Quan_Ly_Do_An.Data;
using System.Data;
using System.Net.Http.Headers;

namespace Quan_Ly_Do_An.Controllers
{
    //Controller của trang Login theo mô hình MVC model view controller thì đây là controller
    public class AccountController : Controller
    {
        private readonly IHttpContextAccessor _contextAccessor;
        //Khai báo AccessToken (lớp)
        private readonly Class.AccessToken _accessToken;
        private readonly DatabaseDoAnContext _dbAnContext;

        public AccountController(IHttpContextAccessor contextAccessor, Class.AccessToken accessToken, DatabaseDoAnContext dbAnContext)
        {
            _contextAccessor = contextAccessor;
            //khởi tạo trong hàm khởi tạo
            _accessToken = accessToken;
            _dbAnContext = dbAnContext;
        }

        //hàm Index trả về view login
        //chuột phải -> go to view
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string gmail)
        {
            //var authUrl = _accessToken.CheckToken(gmail);

            //if (authUrl.StartsWith("https://accounts.google.com"))
            //{
            //    // Chuyển hướng đến Google để đăng nhập nếu chưa có token hợp lệ
            //    return Redirect(authUrl);
            //}

            //// Nếu đã có token, lấy accessToken từ Session
            //var accessToken = _contextAccessor.HttpContext.Session.GetString("AccessToken");

            // Lưu thông tin vào Session
            //_contextAccessor.HttpContext.Session.SetString("UserRole", role);
         
            var user = await _dbAnContext.Users
             .FirstOrDefaultAsync(u => u.Email == gmail);
            if (user != null)
            {
                string studentCode = user.StudentCode;
                int userId = user.UserId;
                string role = user.Role;
                _contextAccessor.HttpContext.Session.SetString("UserName", user.FullName);
                _contextAccessor.HttpContext.Session.SetString("AccessToken","f");
                _contextAccessor.HttpContext.Session.SetString("Email", user.Email);
                _contextAccessor.HttpContext.Session.SetString("StudentCode", studentCode);
                _contextAccessor.HttpContext.Session.SetInt32("UserId", userId);
                _contextAccessor.HttpContext.Session.SetString("Role", role);
                ViewBag.Message = "Authorization successful! Access token obtained.";


                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Account");
        }
        public async Task<IActionResult> Logout()
        {
            _contextAccessor.HttpContext.Session.Clear();
            return RedirectToAction("Index", "Account");
        }


        public JsonResult CheckEmail(string email)
        {
            // Kiểm tra email phải có đuôi là @dtu.edu.vn hoặc @duytan.edu.vn
            if (email.EndsWith("@dtu.edu.vn") || email.EndsWith("@duytan.edu.vn"))
            {
                return Json(true); // Email có đuôi hợp lệ
            }

            return Json(false); // Email không có đuôi hợp lệ
        }


        //hàm Code dùng để lấy Mã xác nhận từ google và lưu các giá trị cần thiết vào session
        //đọc hawocj hỏi GPT để hiểu thêm
        
        public async Task<IActionResult> Code(string code, string state)
        {
            if (string.IsNullOrEmpty(code))
            {
                return new BadRequestObjectResult("No authorization code provided.");
            }

            var clientId = "257480626985-herhg3rvud38ml3fkqph2b4bsr5u3s4p.apps.googleusercontent.com";
            var clientSecret = "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";
            var redirectUri = "https://localhost:7009/Account/Code";

            try
            {
                // Đổi mã code thành token
                var tokenResponse = await _accessToken.ExchangeCodeForTokenAsync(code, clientId, clientSecret, redirectUri);

                if (tokenResponse != null)
                {
                    dynamic json = JsonConvert.DeserializeObject(tokenResponse);
                    string accessToken = json.access_token;
                    var userInfo = await _accessToken.GetUserInfoAsync(accessToken);

                    if (userInfo != null)
                    {
                        string email = userInfo["email"].ToString();
                        //string role = _accessToken.DetermineUserRole(email);
                        string userName = userInfo["name"]?.ToString();

                        // Lưu thông tin vào Session
                        //_contextAccessor.HttpContext.Session.SetString("UserRole", role);
                        _contextAccessor.HttpContext.Session.SetString("UserName", userName);
                        _contextAccessor.HttpContext.Session.SetString("AccessToken", accessToken);
                        _contextAccessor.HttpContext.Session.SetString("Email", email);
                        var user = await _dbAnContext.Users
                         .FirstOrDefaultAsync(u => u.Email == email);
                        if (user != null) { 
                        string studentCode=user.StudentCode;
                        int userId=user.UserId;
                            string role=user.Role;
                            _contextAccessor.HttpContext.Session.SetString("StudentCode", studentCode);
                            _contextAccessor.HttpContext.Session.SetInt32("UserId", userId);
                            _contextAccessor.HttpContext.Session.SetString("Role", role);
                            ViewBag.Message = "Authorization successful! Access token obtained.";
                            

                            return RedirectToAction("Index", "Home");
                        }

                    }
                    else
                    {
                        ViewBag.Message = "Failed to obtain user info.";
                    }
                }
                else
                {
                    ViewBag.Message = "Failed to obtain access token.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction("Index", "Account");
        }
        
    }

}
