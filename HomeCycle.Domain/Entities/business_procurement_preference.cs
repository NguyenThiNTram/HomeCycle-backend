using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Entities
{
    public class business_procurement_preference
    {
        public Guid PreferenceId { get; set; }
        public Guid BusinessProfileId { get; set; }

        public List<string> TargetCities { get; set; }
        public List<int> AcceptableDamageLevels { get; set; }
        public List<int> AcceptableFunctionalityStatuses { get; set; }
        public List<int> ProcurementScales { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public business_procurement_preference()
        {
        }


        public business_procurement_preference(Guid preferenceId, Guid businessProfileId) : this()
        {
            PreferenceId = preferenceId;
            BusinessProfileId = businessProfileId;
        }
    }
}
