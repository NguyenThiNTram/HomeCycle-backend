using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum PriorityLevel
    {
        // Ưu tiên Thấp - Dùng cho các bài đăng thông thường, không vội bán/mua.
        Low = 1,

        // Ưu tiên Trung bình - Mặc định cho mọi bài đăng.
        Medium = 2,

        // Ưu tiên Cao - Cần bán/mua nhanh (ví dụ: Dọn nhà gấp, giải phóng kho).
        High = 3,

        // Khẩn cấp / Cực kỳ ưu tiên - Bài đăng được đẩy lên đầu trang hoặc dán nhãn Khẩn cấp.
        Urgent = 4
    }
}
