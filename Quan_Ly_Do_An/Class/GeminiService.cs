using System.Text.Json;
using System.Text;

namespace Quan_Ly_Do_An.Class
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var geminiConfig = configuration.GetSection("GoogleGeminiApi");
            _apiKey = geminiConfig["ApiKey"];
            _endpoint = geminiConfig["Endpoint"];
        }

        public async Task<string> GenerateContentAsync(string text)
        {
            // Dữ liệu yêu cầu
            var requestBody = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new { text }
                }
            }
        }
            };

            // Chuẩn bị nội dung yêu cầu
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            // Gửi yêu cầu
            var response = await _httpClient.PostAsync($"{_endpoint}?key={_apiKey}", jsonContent);

            response.EnsureSuccessStatusCode();

            // Đọc phản hồi
            var jsonResponse = await response.Content.ReadAsStringAsync();

            try
            {
                // Parse JSON và lấy nội dung text
                var jsonDocument = JsonDocument.Parse(jsonResponse);

                // Điều hướng đến `candidates[0].content.parts[0].text`
                var textContent = jsonDocument
                    .RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return textContent; // Trả về nội dung text
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing JSON response: {ex.Message}\nResponse: {jsonResponse}");
            }
        }

    }
}
