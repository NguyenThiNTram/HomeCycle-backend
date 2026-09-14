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
        public string Name { get; init; } // Tên kiện vật lý, không nhất thiết trùng tên Product.
        public string? Code { get; init; }

        // HomeCycle convention: one item = one physical parcel. Always 1, never Agreement.Quantity.
        public int Quantity { get; init; }

        // chuẩn hóa đơn vị GHN
        public int WeightGram { get; init; }
        public int LengthCm { get; init; }
        public int WidthCm { get; init; }
        public int HeightCm { get; init; }
    }

    // Snapshot kiện hàng nhẹ đã gửi GHN
    public sealed class GhnLightParcelSnapshotDto
    {
        public int WeightGram { get; init; }

        public int LengthCm { get; init; }
        public int WidthCm { get; init; }
        public int HeightCm { get; init; }
    }
}
