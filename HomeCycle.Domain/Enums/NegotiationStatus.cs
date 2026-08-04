using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum NegotiationStatus
    {
        Open = 1,      // Đang thương lượng
        Agreed = 2,    // Đã thống nhất một proposal
        Closed = 3,    // Một bên chủ động kết thúc mà không đạt thỏa thuận
        Expired = 4    // Hết hạn do hệ thống
    }
}
