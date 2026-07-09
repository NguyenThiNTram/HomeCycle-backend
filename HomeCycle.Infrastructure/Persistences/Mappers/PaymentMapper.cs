using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class PaymentMapper
    {
        public static payment ToDomain(this Payment entity)
        {
            if (entity == null) return null;
            return new payment
            {
                PaymentId = entity.PaymentId,
                AgreementId = entity.AgreementId,
                OrderId = entity.OrderId,
                PayerId = entity.PayerId,
                PaymentType = entity.PaymentType,
                PaymentMethod = entity.PaymentMethod,
                Amount = entity.Amount,
                Description = entity.Description,
                PaymentStatus = entity.PaymentStatus,
                PaidAt = entity.PaidAt
            };
        }
        public static Payment ToInfrastructure(this payment entity)
        {
            if (entity == null) return null;
            return new Payment
            {
                PaymentId = entity.PaymentId,
                AgreementId = entity.AgreementId,
                OrderId = entity.OrderId,
                PayerId = entity.PayerId,
                PaymentType = entity.PaymentType,
                PaymentMethod = entity.PaymentMethod,
                Amount = entity.Amount,
                Description = entity.Description,
                PaymentStatus = entity.PaymentStatus,
                PaidAt = entity.PaidAt
            };
        }
    }
}
