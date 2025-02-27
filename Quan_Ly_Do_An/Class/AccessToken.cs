using Newtonsoft.Json;
using System.Web;

namespace Quan_Ly_Do_An.Class
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Azure.Core;
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.PeopleService.v1;
    using Google.Apis.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class AccessToken
    {



        private readonly IHttpContextAccessor _contextAccessor;
     
        public AccessToken(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
           

        }
        //Hàm CheckToken dùng để kiểm tra AccessToken có được cấp hay chưa
        public string CheckToken(string gmail)
        {
            // nếu đã tồn tại và đưuọc lưu vào trong session thì  return accessToken;
            //ngược lại  return authUrl;
            var accessToken = _contextAccessor.HttpContext.Session.GetString("AccessToken");

            if (string.IsNullOrEmpty(accessToken))
            {
                var clientId = "257480626985-herhg3rvud38ml3fkqph2b4bsr5u3s4p.apps.googleusercontent.com";
                var redirectUri = "https://localhost:7009/Account/Code";
                var scope = "https://mail.google.com/ https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile";

                var responseType = "code";
                var accessType = "offline";
                var includeGrantedScopes = "true";
                var state = Guid.NewGuid().ToString();
                var authUrl = "https://accounts.google.com/o/oauth2/v2/auth?" +
                    "scope=" + HttpUtility.UrlEncode(scope) +
                    "&access_type=" + HttpUtility.UrlEncode(accessType) +
                    "&include_granted_scopes=" + HttpUtility.UrlEncode(includeGrantedScopes) +
                    "&response_type=" + HttpUtility.UrlEncode(responseType) +
                    "&client_id=" + HttpUtility.UrlEncode(clientId) +
                    "&redirect_uri=" + HttpUtility.UrlEncode(redirectUri) +
                    "&state=" + HttpUtility.UrlEncode(state) +
                    "&login_hint=" + HttpUtility.UrlEncode(gmail); // Thêm email vào URL

                return authUrl;
            }

            return accessToken;
        }

        //Hàm GetUserInfoAsync dùng để lấy thông tin cơ bản của người đăng nhập ví dụ,tên trong gamil,gmail,...
        public async Task<JObject> GetUserInfoAsync(string accessToken)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.GetStringAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                return JObject.Parse(response);
            }
        }
        //lấy tên user
        //public async Task<string> GetUserNameAsync(string accessToken)
        //{
        //    using (var httpClient = new HttpClient())
        //    {
        //        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        //        // Gửi yêu cầu đến Google API để lấy thông tin người dùng
        //        var response = await httpClient.GetStringAsync("https://www.googleapis.com/oauth2/v2/userinfo");

        //        // Parse dữ liệu JSON trả về
        //        var userInfo = JObject.Parse(response);

        //        // Lấy tên người dùng từ trường "name" trong dữ liệu JSON
        //        var userName = userInfo["name"]?.ToString();

        //        return userName;
        //    }
        //}
        //Hàm này dùng để trả về 1 AccsessToken
        public async Task<string> ExchangeCodeForTokenAsync(string code, string clientId, string clientSecret, string redirectUri)
        {
            var tokenRequestUrl = "https://oauth2.googleapis.com/token";

            using (var client = new HttpClient())
            {
                var tokenRequestBody = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            });

                var tokenResponse = await client.PostAsync(tokenRequestUrl, tokenRequestBody);
                var responseString = await tokenResponse.Content.ReadAsStringAsync();

                if (tokenResponse.IsSuccessStatusCode)
                {
                    return responseString;
                }
                else
                {
                    throw new HttpRequestException($"Failed to exchange code for token: {responseString}");
                }
            }
        }
        //Hàm này dùng để làm mới TOken nếu nó đã hết hạn trong sesion (30p)
        public async Task<string> RefreshAccessTokenAsync(string refreshToken, string clientId, string clientSecret)
        {
            var values = new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "refresh_token", refreshToken },
            { "grant_type", "refresh_token" }
        };

            var content = new FormUrlEncodedContent(values);

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync("https://oauth2.googleapis.com/token", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    dynamic json = JsonConvert.DeserializeObject(responseString);
                    return json.access_token;
                }
                else
                {
                    return null;
                }
            }
        }
        //Hàm phân loại users
        public string DetermineUserRole(string email)
        {
            if (email.EndsWith("@dtu.edu.vn"))
            {
                return "Student"; // Sinh viên
            }
            //@duytan.edu.vn
            else if (email.EndsWith("@gmail.com"))
            {
                return "Instructor"; // Giảng viên
            }

            return "Unknown"; // Nếu không xác định được vai trò
        }
       


    }

}
