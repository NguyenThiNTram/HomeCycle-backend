using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Configs
{
    public class FileUploadPolicyConfigDto
    {
        public List<FileUploadRuleConfigDto> Rules { get; set; } = [];
    }

    public class FileUploadRuleConfigDto
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FileUploadContext Context { get; set; }

        public long MaxFileSizeBytes { get; set; }

        public List<string> AllowedExtensions { get; set; } = new List<string>();
    }

    public static class FileUploadPolicyConstraints
    {
        // Giới hạn an toàn ở tầng ứng dụng cho kích thước của một file.
        // Giới hạn tổng request của web server vẫn cần được cấu hình độc lập.
        public const long MaxConfigurableFileSizeBytes = 25L * 1024 * 1024;
    }

}
