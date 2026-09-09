using FluentValidation;
using HomeCycle.Application.DTOs.Requests.Inspections;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Validations.Inspections
{
    public sealed class ScheduleInspectionCollectionRequestValidator
       : AbstractValidator<ScheduleInspectionCollectionRequest>
    {
        public ScheduleInspectionCollectionRequestValidator()
        {
            RuleFor(x => x.ExpectedRevision)
                .GreaterThan(0)
                .WithMessage("ExpectedRevision không hợp lệ.");

            RuleFor(x => x.CollectionDate)
                .GreaterThan(DateTimeOffset.UtcNow)
                .WithMessage("Thời gian thu gom phải ở tương lai.");

            RuleFor(x => x.DeliveryMethod)
                .Must(x => x is DeliveryMethod.GhnDelivery
                    or DeliveryMethod.SellerDelivers
                    or DeliveryMethod.BuyerPickUp)
                .WithMessage("Phương thức giao nhận không hợp lệ.");

            //RuleFor(x => x.PaymentType)
            //    .Must(x => x is PaymentType.Deposit or PaymentType.Full_Payment)
            //    .WithMessage("Loại thanh toán không hợp lệ.");

            When(x => x.DeliveryMethod == DeliveryMethod.GhnDelivery, () =>
            {
                //RuleFor(x => x.PaymentType)
                //    .Equal(PaymentType.Full_Payment)
                //    .WithMessage("Vận chuyển GHN yêu cầu thanh toán toàn bộ phần còn thiếu.");

                RuleFor(x => x.GhnInfo)
                    .NotNull()
                    .WithMessage("Thiếu thông tin vận chuyển GHN.");

                RuleFor(x => x.EstimatedShippingFee)
                    .Null()
                    .WithMessage("Phí GHN do backend tính, không nhận từ client.");

                When(x => x.GhnInfo != null, () =>
                {
                    RuleFor(x => x.GhnInfo!.PaymentTypeId)
                        .Null()
                        .WithMessage("PaymentTypeId GHN do hệ thống cấu hình.");

                    RuleFor(x => x.GhnInfo!.Quote)
                        .Null()
                        .WithMessage("Quote GHN do backend tính.");

                    RuleFor(x => x.GhnInfo!.QuoteStatus)
                        .Null()
                        .WithMessage("QuoteStatus GHN do backend quản lý.");
                });
            });

            When(
            x => x.DeliveryMethod is DeliveryMethod.SellerDelivers
                or DeliveryMethod.BuyerPickUp,
            () =>
            {
                RuleFor(x => x.GhnInfo)
                    .Null()
                    .WithMessage("Phương thức giao nhận này không sử dụng thông tin GHN.");

                RuleFor(x => x.EstimatedShippingFee)
                    .GreaterThanOrEqualTo(0)
                    .When(x => x.EstimatedShippingFee.HasValue)
                    .WithMessage("Phí giao nhận không được nhỏ hơn 0.");
            });
            //When(x => x.DeliveryMethod == DeliveryMethod.SellerDelivers, () =>
            //{
            //    RuleFor(x => x.GhnInfo)
            //        .Null()
            //        .WithMessage("SellerDelivers không sử dụng thông tin GHN.");

            //    RuleFor(x => x.EstimatedShippingFee)
            //        .GreaterThanOrEqualTo(0)
            //        .When(x => x.EstimatedShippingFee.HasValue)
            //        .WithMessage("Phí giao hàng không được nhỏ hơn 0.");
            //});

            //When(x => x.DeliveryMethod == DeliveryMethod.BuyerPickUp, () =>
            //{
            //    RuleFor(x => x.GhnInfo)
            //        .Null()
            //        .WithMessage("BuyerPickUp không sử dụng thông tin GHN.");

            //    RuleFor(x => x.EstimatedShippingFee)
            //        .Must(x => !x.HasValue || x.Value == 0)
            //        .WithMessage("BuyerPickUp không phát sinh phí vận chuyển.");
            //});
        }
    }
}
