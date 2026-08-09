using HomeCycle.Application.DTOs.Requests.Agreements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.GHN
{
    public sealed class GhnContactSnapshotDto
    {
        public required string FullName { get; init; }
        public required string Phone { get; init; }
        public required GhnAddressSnapshotDto Address { get; init; }
    }

    public sealed class UpdateGhnContactRequest
    {
        public required string FullName { get; init; }
        public required string Phone { get; init; }

        public int ProvinceId { get; init; }
        public int DistrictId { get; init; }
        public required string WardCode { get; init; }

        public required string AddressDetail { get; init; }
    }
}
