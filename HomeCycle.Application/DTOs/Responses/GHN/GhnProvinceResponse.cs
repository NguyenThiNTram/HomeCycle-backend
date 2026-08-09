using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.GHN
{
    public sealed record GhnProvinceResponse(
        int ProvinceId,
        string ProvinceName,
        string? Code,
        int Status);

    public sealed record GhnDistrictResponse(
        int DistrictId,
        int ProvinceId,
        string DistrictName,
        string? Code,
        int Type,
        int SupportType,
        int Status);

    public sealed record GhnWardResponse(
        string WardCode,
        int DistrictId,
        string WardName,
        int SupportType,
        int Status);
}
