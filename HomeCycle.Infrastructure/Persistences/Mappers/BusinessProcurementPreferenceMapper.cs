using HomeCycle.Domain.Entities;
using HomeCycle.Infrastructure.Persistences.Entities;
using MathNet.Numerics.Statistics.Mcmc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class BusinessProcurementPreferenceMapper
    {
        public static business_procurement_preference ToDomain(this Business_Procurement_Preference persistence)
        {
            if (persistence == null) return null!;

            var domain = new business_procurement_preference
            {
                PreferenceId = persistence.PreferenceId,
                BusinessProfileId = persistence.BusinessProfileId,
                TargetCities = persistence.TargetCities.ToList(),
                AcceptableDamageLevels = persistence.AcceptableDamageLevels.ToList(),
                AcceptableFunctionalityStatuses = persistence.AcceptableFunctionalityStatuses.ToList(),
                ProcurementScales = persistence.ProcurementScales.ToList(),
                CreatedAt = persistence.CreatedAt,
                UpdatedAt = persistence.UpdatedAt
            };

            return domain;
        }

        public static Business_Procurement_Preference ToInfrastructure(this business_procurement_preference domain)
        {
            if (domain == null) return null!;

            return new Business_Procurement_Preference
            {
                PreferenceId = domain.PreferenceId,
                BusinessProfileId = domain.BusinessProfileId,
                TargetCities = domain.TargetCities.ToArray(),
                AcceptableDamageLevels = domain.AcceptableDamageLevels.ToArray(),
                AcceptableFunctionalityStatuses = domain.AcceptableFunctionalityStatuses.ToArray(),
                ProcurementScales = domain.ProcurementScales.ToArray(),
                CreatedAt = domain.CreatedAt,
                UpdatedAt = domain.UpdatedAt
            };
        }
    }
}
