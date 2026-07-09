using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class OTPMapper
    {
        public static otp ToDomain(this OTP entity)
        {
            if (entity == null) return null;
            return new otp
            {
                OtpId = entity.OtpId,
                UserId = entity.UserId,
                Code = entity.Code,
                Purpose = entity.Purpose,
                IsUsed = entity.IsUsed,
                ExpiredAt = entity.ExpiredAt,
                UsedAt = entity.UsedAt,
                CreatedAt = entity.CreatedAt
            };
        }
        public static OTP ToInfrastructure(this otp entity)
        {
            if (entity == null) return null;
            return new OTP
            {
                OtpId = entity.OtpId,
                UserId = entity.UserId,
                Code = entity.Code,
                Purpose = entity.Purpose,
                IsUsed = entity.IsUsed,
                ExpiredAt = entity.ExpiredAt,
                UsedAt = entity.UsedAt,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
