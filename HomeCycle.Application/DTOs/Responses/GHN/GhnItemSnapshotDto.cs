using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.GHN
{
    public sealed class GhnItemSnapshotDto
    {
        //Snapshot kiện hàng đã gửi GHN
        public string Name { get; init; } //product name
        public string? Code { get; init; }

        public int Quantity { get; init; }

        // chuẩn hóa đơn vị GHN
        public int WeightGram { get; init; }
        public int LengthCm { get; init; }
        public int WidthCm { get; init; }
        public int HeightCm { get; init; }
    }

    //request chỉnh sửa phải nằm trong Requests.Agreements
    //public sealed class UpdateGhnItemRequest
    //{
    //    public int Quantity { get; init; }

    //    public int WeightGram { get; init; }
    //    public int LengthCm { get; init; }
    //    public int WidthCm { get; init; }
    //    public int HeightCm { get; init; }
    //}

    // Snapshot kiện hàng nhẹ đã gửi GHN
    public sealed class GhnLightParcelSnapshotDto
    {
        public int WeightGram { get; init; }

        public int LengthCm { get; init; }
        public int WidthCm { get; init; }
        public int HeightCm { get; init; }
    }
}
