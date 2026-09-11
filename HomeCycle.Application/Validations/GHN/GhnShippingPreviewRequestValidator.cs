using FluentValidation;
using HomeCycle.Application.DTOs.Requests.GHN;
using HomeCycle.Application.DTOs.Responses.GHN;

namespace HomeCycle.Application.Validations.GHN;

public sealed class GhnShippingPreviewRequestValidator : AbstractValidator<GhnShippingPreviewRequest>
{
    public GhnShippingPreviewRequestValidator()
    {
        RuleFor(x => x.Sender).NotNull().SetValidator(new ContactValidator()!);
        RuleFor(x => x.Receiver).NotNull().SetValidator(new ContactValidator()!);
        RuleFor(x => x.ServiceTypeId).Must(x => x is 2 or 5)
            .WithMessage("ServiceTypeId chỉ nhận 2 (dưới 20kg, một kiện) hoặc 5 (từ 20kg hoặc nhiều kiện).");
        RuleFor(x => x.ParcelCount).GreaterThan(0);
        RuleFor(x => x.RequiredNote).NotEmpty().Must(x => x != null &&
            new[] { "CHOTHUHANG", "CHOXEMHANGKHONGTHU", "KHONGCHOXEMHANG" }
                .Contains(x.Trim(), StringComparer.OrdinalIgnoreCase));
        RuleFor(x => x.Content).MaximumLength(2000);
        RuleFor(x => x.WeightGram).Must(x => x is null or (>= 1 and <= 50_000));
        RuleFor(x => x.LengthCm).Must(x => x is null or (>= 1 and <= 200));
        RuleFor(x => x.WidthCm).Must(x => x is null or (>= 1 and <= 200));
        RuleFor(x => x.HeightCm).Must(x => x is null or (>= 1 and <= 200));
        RuleFor(x => x.Items).NotNull();
        When(x => x.Items != null, () =>
        {
            RuleForEach(x => x.Items).NotNull();
            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(x => x.Name).NotEmpty().MaximumLength(512);
                item.RuleFor(x => x.Quantity).GreaterThan(0);
                item.RuleFor(x => x.WeightGram).GreaterThanOrEqualTo(0);
                item.RuleFor(x => x.LengthCm).InclusiveBetween(0, 200);
                item.RuleFor(x => x.WidthCm).InclusiveBetween(0, 200);
                item.RuleFor(x => x.HeightCm).InclusiveBetween(0, 200);
            });
        });
        When(x => x.ServiceTypeId == 5 && x.Items != null, () =>
        {
            RuleForEach(x => x.Items).SetValidator(new CalculateGhnFeeItemRequestValidator());
        });
        When(x => x.ServiceTypeId == 2, () =>
        {
            RuleFor(x => x.ParcelCount).Equal(1).WithMessage("Nhiều kiện phải dùng ServiceTypeId = 5.");
            RuleFor(x => x.WeightGram).Must(x => x is null or < 20_000)
                .WithMessage("Hàng nhẹ phải có tổng khối lượng dưới 20.000 gram.");
        });
    }

    private sealed class ContactValidator : AbstractValidator<GhnContactSnapshotDto>
    {
        public ContactValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(1024);
            RuleFor(x => x.Phone).NotEmpty();
            RuleFor(x => x.Address).NotNull();
            When(x => x.Address != null, () =>
            {
                RuleFor(x => x.Address.AddressDetail).NotEmpty().MaximumLength(1024);
                RuleFor(x => x.Address.DistrictId).GreaterThan(0);
                RuleFor(x => x.Address.WardCode).NotEmpty();
                RuleFor(x => x.Address.WardName).NotEmpty();
                RuleFor(x => x.Address.DistrictName).NotEmpty();
                RuleFor(x => x.Address.ProvinceName).NotEmpty();
            });
        }
    }
}
