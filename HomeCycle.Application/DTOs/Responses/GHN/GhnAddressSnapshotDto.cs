using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.GHN
{
    public sealed class GhnAddressSnapshotDto
    {
        public int ProvinceId { get; init; }
        public string ProvinceName { get; init; }

        public int DistrictId { get; init; }
        public string DistrictName { get; init; }

        public string WardCode { get; init; }
        public string WardName { get; init; }

        // Dùng dựng from_address/to_address
        public string? AddressDetail { get; init; }
    }
}
