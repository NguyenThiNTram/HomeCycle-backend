using Firebase.Storage;
using HomeCycle.Application.Interfaces.Services.Externals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals
{
    public class FirebaseStorageService : IFileStorageService
    {
        private readonly string _bucketName = "gs://homecycle-platform-fb8cb.firebasestorage.app";
        private readonly string _folderName = "uploads";

        public async Task<string> UploadFileAsync(Stream fileStream, string originalFileName)
        {
            // 1. Tạo tên file duy nhất bằng Guid phối hợp định dạng gốc để tránh trùng tên trên Cloud
            string extension = Path.GetExtension(originalFileName);
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // 2. Khởi tạo Firebase Storage
            var storage = new FirebaseStorage(_bucketName);

            // 3. Thực hiện đẩy stream file lên thư mục 'uploads'
            await storage
                .Child(_folderName)
                .Child(uniqueFileName)
                .PutAsync(fileStream);

            // 4. CHỈ trả về tên file duy nhất để lưu vào PostgreSQL
            return uniqueFileName;
        }

        public string GetFileUrl(string storedFileName)
        {
            if (string.IsNullOrEmpty(storedFileName)) return string.Empty;

            // Cấu hình URL chuẩn hiển thị trực tiếp của Firebase Storage không cần qua thư viện
            // Định dạng: https://googleapis.com{bucket}/o/{folder}%2F{filename}?alt=media
            return $"https://googleapis.com{_bucketName}/o/{_folderName}%2F{storedFileName}?alt=media";
        }
    }
}
