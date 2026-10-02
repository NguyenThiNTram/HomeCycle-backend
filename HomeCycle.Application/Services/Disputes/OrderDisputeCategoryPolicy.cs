using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Services.Disputes
{
    public static class OrderDisputeCategoryPolicy
    {
        // Sự cố vận chuyển GHN (chỉ đơn GHN):
        // - ITEM_NOT_RECEIVED: hàng bị mất / thất lạc            -> hoàn tiền hàng + phí ship
        // - DAMAGED_OR_LOST:   hàng bị hư hỏng                   -> hoàn tiền hàng, không hoàn phí ship
        // - SHIPPER_RETURNED:  shipper tự ý hủy / hoàn đơn        -> hoàn tiền hàng + phí ship
        // - BUYER_REFUSED:     khách không nhận hàng (người bán mở) -> hoàn tiền hàng, không hoàn phí ship
        private static readonly HashSet<string> GhnCarrierCategoryCodes = new()
        {
            "ITEM_NOT_RECEIVED",
            "DAMAGED_OR_LOST",
            "SHIPPER_RETURNED",
            "BUYER_REFUSED"
        };

        private static readonly HashSet<string> ShippingFeeRefundCategoryCodes = new()
        {
            "ITEM_NOT_RECEIVED",
            "SHIPPER_RETURNED"
        };

        public static bool IsAllowed(string code, bool noShowEligible, DeliveryMethod? deliveryMethod, bool senderIsBuyer)
        {
            var normalizedCode = Normalize(code);

            return normalizedCode switch
            {
                "NO_SHOW" => noShowEligible,

                // Đơn GHN: người mua hủy được tới trước khi GHN lấy hàng nên không còn dùng danh mục này.
                "SELLER_NOT_SHIPPED" => false,

                "BUYER_REFUSED"
                    => deliveryMethod == DeliveryMethod.GhnDelivery && !senderIsBuyer,

                "ITEM_NOT_RECEIVED" or
                "DAMAGED_OR_LOST" or
                "SHIPPER_RETURNED"
                    => deliveryMethod == DeliveryMethod.GhnDelivery && senderIsBuyer,

                "ABUSIVE_REVIEW" => false,

                _ => true
            };
        }

        // Khiếu nại sự cố GHN gửi thẳng cho kiểm duyệt viên, không qua bước hai bên tự xử lý.
        public static bool IsGhnCarrierCategory(string? code) =>
            code != null && GhnCarrierCategoryCodes.Contains(Normalize(code));

        // Danh mục được hoàn thêm phí ship GHN khi kết luận có lợi cho người mua.
        public static bool RefundsShippingFee(string? code) =>
            code != null && ShippingFeeRefundCategoryCodes.Contains(Normalize(code));

        private static string Normalize(string code) => code.Trim().ToUpperInvariant();
    }
}
