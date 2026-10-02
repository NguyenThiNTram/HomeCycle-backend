using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Carts
{
    public sealed class CartUpdatedResponse
    {
        // Thời điểm server đọc snapshot giỏ hàng sau khi commit (UTC).
        // Client bỏ qua event có UpdatedAt cũ hơn dữ liệu giỏ đang hiển thị.
        public DateTime UpdatedAt { get; init; }

        // Snapshot giỏ hàng. Null khi server không đọc được snapshot:
        // lúc đó client chỉ coi event là tín hiệu và gọi lại GET /cart.
        public IReadOnlyList<CartRealtimeItemResponse>? Items { get; init; }
        public int? TotalQuantity { get; init; }
        public decimal? TotalPrice { get; init; }
    }

    // Bản rút gọn của CartItemResponse, chỉ giữ các field màn giỏ hàng cần.
    // Enum được gửi dạng chuỗi giống REST (SignalR mặc định gửi enum dạng số).
    public sealed class CartRealtimeItemResponse
    {
        public Guid CartItemId { get; init; }
        public Guid PostId { get; init; }
        public int Quantity { get; init; }
        public DateTime AddedAt { get; init; }

        public Guid OwnerId { get; init; }
        public string? ProductName { get; init; }
        public string? ProductTypeName { get; init; }
        public string? CategoryName { get; init; }
        public string? BrandName { get; init; }
        public decimal? BasePrice { get; init; }
        public int? RemainingQuantity { get; init; }
        public string? PostType { get; init; }
        public string? Status { get; init; }
        public string? ThumbnailUrl { get; init; }
    }
}
