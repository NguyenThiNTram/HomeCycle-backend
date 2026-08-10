using HomeCycle.Application.DTOs.Responses.GHN;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Requests.Agreements
{
    public class AgreementDetailsDto
    {
        public int Revision { get; init; } = 1; // Tăng mỗi lần nội dung Agreement thay đổi
        public string? Notes { get; set; }

        // --- NHÓM 1: DÀNH CHO CÓ KIỂM ĐỊNH (Inspection_Trade) ---
        public DateTime? InspectionDate { get; set; }
        public string? InspectionAddress { get; set; }

        // --- NHÓM 2: DÀNH CHO THU GOM / GIAO HÀNG (Direct_Collection, GHN_Delivery) ---
        public DateTime? CollectionDate { get; set; }
        public string? PickupAddress { get; set; }
        public string? DeliveryAddress { get; set; }

        public DeliveryMethod? DeliveryMethod { get; set; }

        // ===== DeliveryMethod == GhnDelivery =====
        // Thông tin vận chuyển GHN (sender/receiver, service type, items...) dùng để tính phí
        public GhnShippingInfo? GhnInfo { get; set; }

        // Tiền thu hộ cho người gửi (COD)
        public int? CodValue { get; set; }

        // Phí ship do gọi API GHN (hoặc tự thỏa thuận) trả về lúc cấu hình form
        public decimal? EstimatedShippingFee { get; set; }
    }
}
