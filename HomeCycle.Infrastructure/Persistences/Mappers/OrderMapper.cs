using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class OrderMapper
    {
        public static order ToDomain(this Order entity)
        {
            if (entity == null) return null;
            return new order
            {
                OrderId = entity.OrderId,
                AgreementId = entity.AgreementId,
                PostId = entity.PostId,
                ProductName = entity.ProductName,
                Quantity = entity.Quantity,
                OriginalTotalAmount = entity.OriginalTotalAmount,
                FinalTotalAmount = entity.FinalTotalAmount,
                AmountPaid = entity.AmountPaid,
                AmountRemaining = entity.AmountRemaining,
                PaymentStatus = entity.PaymentStatus,
                OrderStatus = entity.OrderStatus,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static Order ToInfrastructure(this order entity)
        {
            if (entity == null) return null;
            return new Order
            {
                OrderId = entity.OrderId,
                AgreementId = entity.AgreementId,
                PostId = entity.PostId,
                ProductName = entity.ProductName,
                Quantity = entity.Quantity,
                OriginalTotalAmount = entity.OriginalTotalAmount,
                FinalTotalAmount = entity.FinalTotalAmount,
                AmountPaid = entity.AmountPaid,
                AmountRemaining = entity.AmountRemaining,
                PaymentStatus = entity.PaymentStatus,
                OrderStatus = entity.OrderStatus,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
