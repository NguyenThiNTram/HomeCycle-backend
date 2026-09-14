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
        public static bool IsAllowed(string code, bool hasAppointments, DeliveryMethod? deliveryMethod)
        {
            var normalizedCode = code.Trim().ToUpperInvariant();

            return normalizedCode switch
            {
                "NO_SHOW" => hasAppointments,

                "SELLER_NOT_SHIPPED" or
                "DAMAGED_OR_LOST" or
                "ITEM_NOT_RECEIVED"
                    => deliveryMethod == DeliveryMethod.GhnDelivery,

                "ABUSIVE_REVIEW" => false,

                _ => true
            };
        }
    }
}
