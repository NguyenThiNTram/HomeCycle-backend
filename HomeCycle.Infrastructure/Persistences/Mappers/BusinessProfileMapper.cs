using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class BusinessProfileMapper
    {
        public static business_profile ToDomain(this Business_Profile entity)
        {
            if (entity == null) return null;
            return new business_profile
            {
                BusinessProfileId = entity.BusinessProfileId,
                UserId = entity.UserId,
                BusinessName = entity.BusinessName,
                BusinessDescription = entity.BusinessDescription,
                TaxCode = entity.TaxCode,
                BusinessAddress = entity.BusinessAddress,
                Ward = entity.Ward,
                City = entity.City,
                OperatingScope = entity.OperatingScope,
                BusinessModel = entity.BusinessModel,
                ReputationScore = entity.ReputationScore,
                VerifiedBy = entity.VerifiedBy,
                VerifiedAt = entity.VerifiedAt,
                RejectReason = entity.RejectReason,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Status = entity.Status,
                IdentityNumber = entity.IdentityNumber,
                FullName = entity.FullName
            };
        }
        public static Business_Profile ToInfrastructure(this business_profile entity)
        {
            if (entity == null) return null;
            return new Business_Profile
            {
                BusinessProfileId = entity.BusinessProfileId,
                UserId = entity.UserId,
                BusinessName = entity.BusinessName,
                BusinessDescription = entity.BusinessDescription,
                TaxCode = entity.TaxCode,
                BusinessAddress = entity.BusinessAddress,
                Ward = entity.Ward,
                City = entity.City,
                OperatingScope = entity.OperatingScope,
                BusinessModel = entity.BusinessModel,
                ReputationScore = entity.ReputationScore,
                VerifiedBy = entity.VerifiedBy,
                VerifiedAt = entity.VerifiedAt,
                RejectReason = entity.RejectReason,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Status = entity.Status,
                IdentityNumber = entity.IdentityNumber,
                FullName = entity.FullName
            };
        }
    }
}
