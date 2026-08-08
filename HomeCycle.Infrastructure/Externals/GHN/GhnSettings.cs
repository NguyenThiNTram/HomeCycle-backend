using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.GHN
{
    public sealed class GhnSettings
    {
        public const string SectionName = "GHNSettings";

        public string BaseUrl { get; set; }
        public string Token { get; set; }
        public int ShopId { get; set; }
        public int TimeoutSeconds { get; init; } = 60;
        public int AddressCacheHours { get; init; } = 12;
    }
}
