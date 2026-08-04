using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum FunctionalityStatus
    {
        FullyFunctional = 0,  // Hoạt động hoàn hảo (Không có lỗi kỹ thuật)
        PartiallyFunctional = 1, // Hoạt động một phần (Có lỗi nhẹ nhưng không ảnh hưởng cốt lõi)
        NonFunctional = 2     // Không hoạt động / Hỏng nặng (Cần sửa chữa mới dùng được)
    }
}
