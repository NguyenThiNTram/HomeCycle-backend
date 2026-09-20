using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    public sealed class GhnSettings
    {
        public const string SectionName = "GHNSettings";

        public string BaseUrl { get; set; }
        public string Token { get; set; }
        public int ShopId { get; set; }
        public int TimeoutSeconds { get; init; } = 60;
        public int AddressCacheHours { get; init; } = 12;

        // Bảo vệ callback thật bằng custom header cấu hình trên GHN.
        // Giá trị thật chỉ được truyền qua biến môi trường, không lưu trong source.
        public string? WebhookSecret { get; init; }

        // Chỉ cho phép Admin gửi callback mô phỏng khi backend không chạy Production.
        public bool EnableWebhookDemo { get; init; } = false;

        // Worker tự tạo đơn GHN (hosted service) chạy nền
        public int CreationWorkerPollSeconds { get; init; } = 10;
        public int CreationWorkerBatchSize { get; init; } = 20;

        // Đơn Processing kẹt (worker chết giữa chừng) được claim lại sau bao nhiêu giây
        public int CreationWorkerReclaimAfterSeconds { get; init; } = 300;
    }
}
