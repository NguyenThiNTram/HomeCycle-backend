using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class RefreshTokenMapper
    {
        public static refresh_token ToDomain(this Refresh_Token entity)
        {
            if (entity == null) return null;
            return new refresh_token
            {
                RefreshTokenId = entity.RefreshTokenId,
                UserId = entity.UserId,
                Token = entity.Token,
                ReplacedByToken = entity.ReplacedByToken,
                RevokedAt = entity.RevokedAt,
                ExpiredAt = entity.ExpiredAt,
                CreatedAt = entity.CreatedAt
            };
        }
        public static Refresh_Token ToInfrastructure(this refresh_token entity)
        {
            if (entity == null) return null;
            return new Refresh_Token
            {
                RefreshTokenId = entity.RefreshTokenId,
                UserId = entity.UserId,
                Token = entity.Token,
                ReplacedByToken = entity.ReplacedByToken,
                RevokedAt = entity.RevokedAt,
                ExpiredAt = entity.ExpiredAt,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
