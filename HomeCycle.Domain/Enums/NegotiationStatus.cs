using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum NegotiationStatus
    {
        Open = 0,        // Đang thương lượng
        Agreed = 1,      // Đã đạt thỏa thuận
        Closed = 2,      // Đóng (hủy hoặc kết thúc)
        Expired = 3,     // Hết hạn
        Unavailable = 4  // Không còn khả dụng (bài đăng hết hàng/ngưng hoạt động)
    }
}
