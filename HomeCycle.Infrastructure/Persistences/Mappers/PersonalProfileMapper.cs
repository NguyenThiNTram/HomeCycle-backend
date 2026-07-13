using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class PersonalProfileMapper
    {
        public static personal_profile ToDomain(this Personal_Profile entity)
        {
            if (entity == null) return null;
            return new personal_profile
            {
                PersonalProfileId = entity.PersonalProfileId,
                UserId = entity.UserId,
                FullName = entity.FullName,
                RepresentativeCode = entity.RepresentativeCode,
                RepresentativeName = entity.RepresentativeName,
                RepresentativeDob = entity.RepresentativeDob,
                RepresentativeAddress = entity.RepresentativeAddress,
                FrontIDCardImage = entity.FrontIDCardImage,
                BackIDCardImage = entity.BackIDCardImage,
                VerificationStatus = (VerifyStatus)entity.VerificationStatus,
                VerifiedBy = entity.VerifiedBy,
                VerifiedAt = entity.VerifiedAt,
                ReputationScore = entity.ReputationScore,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Personal_Profile ToInfrastructure(this personal_profile entity)
        {
            if (entity == null) return null;
            return new Personal_Profile
            {
                PersonalProfileId = entity.PersonalProfileId,
                UserId = entity.UserId,
                FullName = entity.FullName,
                RepresentativeCode = entity.RepresentativeCode,
                RepresentativeName = entity.RepresentativeName,
                RepresentativeDob = entity.RepresentativeDob,
                RepresentativeAddress = entity.RepresentativeAddress,
                FrontIDCardImage = entity.FrontIDCardImage,
                BackIDCardImage = entity.BackIDCardImage,
                VerificationStatus = (int)entity.VerificationStatus,
                VerifiedBy = entity.VerifiedBy,
                VerifiedAt = entity.VerifiedAt,
                ReputationScore = entity.ReputationScore,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
