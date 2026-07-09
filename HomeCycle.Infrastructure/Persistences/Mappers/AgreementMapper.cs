using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeCycle.Domain.Entities;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class AgreementMapper
    {
        public static agreement_form ToDomain(Agreement_Form entity)
        {
            return new agreement_form
            {
                AgreementId = entity.AgreementId,
                NegotiationId = entity.NegotiationId,
                PostId = entity.PostId,
                SellerId = entity.SellerId,
                BuyerId = entity.BuyerId,
                PSnapshot = entity.PSnapshot,
                Quantity = entity.Quantity,
                InitialPrice = entity.InitialPrice,
                FinalPrice = entity.FinalPrice,
                AgreementType = entity.AgreementType,
                AgreementDetailsJsonb = entity.AgreementDetailsJsonb,
                PaymentType = entity.PaymentType,
                AgreementStatus = entity.AgreementStatus,
                CreatedAt = entity.CreatedAt,
                BuyerConfirmedAt = entity.BuyerConfirmedAt,
                SellerConfirmedAt = entity.SellerConfirmedAt
            };
        }

        public static Agreement_Form ToInfrastructure(agreement_form entity)
        {
            return new Agreement_Form
            {
                AgreementId = entity.AgreementId,
                NegotiationId = entity.NegotiationId,
                PostId = entity.PostId,
                SellerId = entity.SellerId,
                BuyerId = entity.BuyerId,
                PSnapshot = entity.PSnapshot,
                Quantity = entity.Quantity,
                InitialPrice = entity.InitialPrice,
                FinalPrice = entity.FinalPrice,
                AgreementType = entity.AgreementType,
                AgreementDetailsJsonb = entity.AgreementDetailsJsonb,
                PaymentType = entity.PaymentType,
                AgreementStatus = entity.AgreementStatus,
                CreatedAt = entity.CreatedAt,
                BuyerConfirmedAt = entity.BuyerConfirmedAt,
                SellerConfirmedAt = entity.SellerConfirmedAt
            };
        }
    }
}
