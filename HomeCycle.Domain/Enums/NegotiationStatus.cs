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
        AgreementPending = 3, // Đang chờ đối tác ký kết thỏa thuận
        Completed = 4, // Đã hoàn tất thương lượng và ký kết thỏa thuận
        Closed = 5,    // Một bên chủ động kết thúc mà không đạt thỏa thuận
        Expired = 6    // Hết hạn do hệ thống
    }
}
