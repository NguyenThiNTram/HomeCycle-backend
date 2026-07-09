using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class PaymentTransactionMapper
    {
        public static payment_transaction ToDomain(this Payment_Transaction entity)
        {
            if (entity == null) return null;
            return new payment_transaction
            {
                PaymentTransactionId = entity.PaymentTransactionId,
                PaymentId = entity.PaymentId,
                UserId = entity.UserId,
                PayOSOrderCode = entity.PayOSOrderCode,
                PayOSPaymentLinkId = entity.PayOSPaymentLinkId,
                PayOSTransactionId = entity.PayOSTransactionId,
                CheckoutUrl = entity.CheckoutUrl,
                PaymentTransactionStatus = entity.PaymentTransactionStatus,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static Payment_Transaction ToInfrastructure(this payment_transaction entity)
        {
            if (entity == null) return null;
            return new Payment_Transaction
            {
                PaymentTransactionId = entity.PaymentTransactionId,
                PaymentId = entity.PaymentId,
                UserId = entity.UserId,
                PayOSOrderCode = entity.PayOSOrderCode,
                PayOSPaymentLinkId = entity.PayOSPaymentLinkId,
                PayOSTransactionId = entity.PayOSTransactionId,
                CheckoutUrl = entity.CheckoutUrl,
                PaymentTransactionStatus = entity.PaymentTransactionStatus,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
