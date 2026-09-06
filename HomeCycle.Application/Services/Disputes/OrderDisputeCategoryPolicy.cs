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
        public static IReadOnlyList<DisputeCategory> BuildAllowedCategories(
                bool hasAppointments,
                DeliveryMethod? deliveryMethod)
        {
            var categories = new List<DisputeCategory>();

            if (hasAppointments)
            {
                categories.Add(DisputeCategory.NoShow);
            }

            categories.Add(DisputeCategory.ItemMismatch);

            if (deliveryMethod == DeliveryMethod.GhnDelivery)
            {
                categories.Add(DisputeCategory.SellerNotShipped);

                categories.Add(DisputeCategory.DamagedOrLost);

                categories.Add(DisputeCategory.ItemNotReceived);
            }

            categories.Add(DisputeCategory.FraudOrScam);

            categories.Add(DisputeCategory.PaymentNotCompleted);

            categories.Add(DisputeCategory.CommitmentViolation);

            categories.Add(DisputeCategory.Other);

            return categories;
        }

        public static bool IsAllowed(
            DisputeCategory category,
            bool hasAppointments,
            DeliveryMethod? deliveryMethod)
        {
            return BuildAllowedCategories(
                    hasAppointments,
                    deliveryMethod)
                .Contains(category);
        }
    }
}
