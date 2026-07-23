using Firebase.Storage;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util;
using Google.Apis.Json;
using System.IO;
using HomeCycle.Application.Interfaces.Services.Externals;
using Microsoft.Extensions.Configuration;
using Supabase.Storage.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals
{
    public class FirebaseStorageService : IFileStorageService
    {
        private readonly string _bucketName;
        private readonly StorageClient _storageClient;

        public FirebaseStorageService(IConfiguration configuration)
        {
            _bucketName = configuration["Firebase:Bucket"]
            ?? throw new ArgumentNullException("Không tìm thấy cấu hình Firebase:Bucket trong appsettings.json");

            string type = configuration["Firebase:CredentialPath:type"];
            string projectId = configuration["Firebase:CredentialPath:project_id"];
            string privateKeyId = configuration["Firebase:CredentialPath:private_key_id"];
            string privateKey = configuration["Firebase:CredentialPath:private_key"];
            string clientEmail = configuration["Firebase:CredentialPath:client_email"];

            // Kiểm tra dữ liệu đầu vào chống Null
            if (string.IsNullOrEmpty(type))
                throw new Exception("Lỗi: Không đọc được cấu hình Firebase:CredentialPath:type. Hãy kiểm tra lại file appsettings.json.");
            if (string.IsNullOrEmpty(privateKey))
                throw new Exception("Lỗi: Không đọc được cấu hình Firebase:CredentialPath:private_key.");

            // Lấy chuỗi JSON cấu hình từ appsettings.json
            var credentialsSection = configuration.GetSection("Firebase:CredentialPath");
            if (!credentialsSection.Exists())
                throw new InvalidOperationException("Không tìm thấy cấu hình Firebase:CredentialPath.");

            var credentialParameters = new Dictionary<string, string>();

            var parameters = new JsonCredentialParameters
            {
                Type = type,
                ProjectId = projectId,
                PrivateKeyId = privateKeyId,
                PrivateKey = privateKey.Replace("\\n", "\n"), // Giữ nguyên cấu trúc xuống dòng của khóa bí mật
                ClientEmail = clientEmail
            };

            try
            {
                // Sử dụng CredentialFactory thay thế theo đúng cảnh báo CS0618
                GoogleCredential credential = CredentialFactory
                    .FromJsonParameters<ServiceAccountCredential>(parameters)
                    .ToGoogleCredential();

                _storageClient = StorageClient.Create(credential);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi kết nối Firebase Service Account: {ex.Message}", ex);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string originalFileName, string folderName, bool overwrite = false)
        {
            string extension = Path.GetExtension(originalFileName);
            //string objectName = $"{folderName}/{Guid.NewGuid()}{extension}";
            string objectName = overwrite
                ? $"{folderName}/{Path.GetFileNameWithoutExtension(originalFileName)}{extension}"
                : $"{folderName}/{Guid.NewGuid()}{extension}";


            //var storageObject = await _storageClient.UploadObjectAsync(
            //    bucket: _bucketName,
            //    objectName: objectName,
            //    contentType: "image/jpeg", // Hoặc dựa vào extension để map mime-type
            //    source: fileStream
            //);
            string contentType = extension switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                _ => "image/jpeg"
            };

            await _storageClient.UploadObjectAsync(
                bucket: _bucketName,
                objectName: objectName,
                contentType: contentType,
                source: fileStream
            );

            // Trả về đường dẫn ảnh public
            //return $"https://googleapis.com{_bucketName}/o/{Uri.EscapeDataString(objectName)}?alt=media";
            return GetFileUrl(objectName, string.Empty);
        }

        public string GetFileUrl(string storedFileName, string folderName)
        {
            if (string.IsNullOrEmpty(storedFileName)) return string.Empty;

            // Kết hợp thư mục và tên file thành đường dẫn đầy đủ trong Storage (vd: avatars/cd0bba05-db5e.jpg)
            //string fullPath = string.IsNullOrEmpty(folderName)
            //    ? storedFileName
            //    : $"{folderName}/{storedFileName}";
            string fullPath = string.IsNullOrEmpty(folderName)
                ? storedFileName
                : $"{folderName.TrimEnd('/')}/{storedFileName.TrimStart('/')}";

            // Mã hóa toàn bộ đường dẫn để đảm bảo URL hợp lệ (tự động biến / thành %2F và xử lý khoảng trắng, ký tự lạ)
            string escapedPath = Uri.EscapeDataString(fullPath);

            return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{escapedPath}?alt=media";
        }

    }
}
