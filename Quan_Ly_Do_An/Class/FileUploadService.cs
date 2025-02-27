namespace Quan_Ly_Do_An.Class
{
    public class FileUploadService
    {
        private readonly string _uploadsFolder;

        public FileUploadService()
        {
            // Cài đặt thư mục lưu trữ file
            _uploadsFolder = Path.Combine("wwwroot", "uploads");

            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File không hợp lệ");
            }

            // Tạo tên file duy nhất
            string fileName = $"{Guid.NewGuid()}_{file.FileName}";

            // Đường dẫn lưu file
            string filePath = Path.Combine(_uploadsFolder, fileName);

            // Lưu file vào thư mục uploads
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Trả về đường dẫn tương đối của file đã lưu
            return Path.Combine("uploads", fileName);
        }
    }
}
