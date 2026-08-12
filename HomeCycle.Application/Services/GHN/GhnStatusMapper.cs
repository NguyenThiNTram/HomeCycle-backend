using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.GHN
{
    public static class GhnStatusMapper
    {
        // Chuyển mã trạng thái GHN thành trạng thái nghiệp vụ 
        // Trả null nếu GHN gửi trạng thái chưa được hệ thống hỗ trợ
        public static ShipmentStatus? Map(string? ghnStatusCode)
        {
            if (string.IsNullOrWhiteSpace(ghnStatusCode))
                return null;

            var normalizedStatus = ghnStatusCode
                .Trim()
                .ToLowerInvariant();

            return normalizedStatus switch
            {
                "ready_to_pick"
                    or "picking"
                    or "money_collect_picking"
                    => ShipmentStatus.ReadyToPick,

                "picked"
                    or "storing"
                    or "transporting"
                    or "sorting"
                    or "delivering"
                    or "money_collect_delivering"
                    or "delivery_fail"
                    => ShipmentStatus.Delivering,

                "delivered"
                    => ShipmentStatus.Delivered,

                "cancel"
                    => ShipmentStatus.Cancelled,

                "waiting_to_return"
                    or "return"
                    or "return_transporting"
                    or "return_sorting"
                    or "returning"
                    or "return_fail"
                    => ShipmentStatus.Returning,

                "returned"
                    => ShipmentStatus.Returned,

                "damage"
                    or "lost"
                    => ShipmentStatus.Damage_Lost,

                "exception"
                    => ShipmentStatus.Exception,

                _ => null
            };
        }
    }
}
