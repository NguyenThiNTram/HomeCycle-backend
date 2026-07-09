using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class PlatformPolicyMapper
    {
        public static platform_policy ToDomain(this Platform_Policy entity)
        {
            if (entity == null) return null;
            return new platform_policy
            {
                PolicyId = entity.PolicyId,
                PolicyType = entity.PolicyType,
                Title = entity.Title,
                Content = entity.Content,
                Version = entity.Version,
                IsActive = entity.IsActive,
                UpdatedAt = entity.UpdatedAt
            };
        }
        public static Platform_Policy ToInfrastructure(this platform_policy entity)
        {
            if (entity == null) return null;
            return new Platform_Policy
            {
                PolicyId = entity.PolicyId,
                PolicyType = entity.PolicyType,
                Title = entity.Title,
                Content = entity.Content,
                Version = entity.Version,
                IsActive = entity.IsActive,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
