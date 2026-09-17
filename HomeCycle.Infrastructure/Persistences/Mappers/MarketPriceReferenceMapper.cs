using HomeCycle.Domain.Entities;

namespace HomeCycle.Infrastructure.Persistences.Mappers;

public static class MarketPriceReferenceMapper
{
    public static market_price_reference ToDomain(this Market_Price_Reference entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new market_price_reference
        {
            Id = entity.Id,
            ProductTypeId = entity.ProductTypeId,
            BrandId = entity.BrandId,
            ModelNumber = entity.ModelNumber,
            NormalizedModelNumber = entity.NormalizedModelNumber,
            ProductName = entity.ProductName,
            KeySpecifications = entity.KeySpecifications,
            HasVariants = entity.HasVariants,
            PriceVndPerUnit = entity.PriceVndPerUnit,
            SourceName = entity.SourceName,
            SourceUrl = entity.SourceUrl,
            ObservedAt = entity.ObservedAt,
            IsVerified = entity.IsVerified,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static Market_Price_Reference ToInfrastructure(this market_price_reference domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new Market_Price_Reference
        {
            Id = domain.Id,
            ProductTypeId = domain.ProductTypeId,
            BrandId = domain.BrandId,
            ModelNumber = domain.ModelNumber,
            NormalizedModelNumber = domain.NormalizedModelNumber,
            ProductName = domain.ProductName,
            KeySpecifications = domain.KeySpecifications,
            HasVariants = domain.HasVariants,
            PriceVndPerUnit = domain.PriceVndPerUnit,
            SourceName = domain.SourceName,
            SourceUrl = domain.SourceUrl,
            ObservedAt = domain.ObservedAt,
            IsVerified = domain.IsVerified,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };
    }
}
