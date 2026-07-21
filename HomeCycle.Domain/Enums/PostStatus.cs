using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum PostStatus
    {
        Draft = 0, //bản nháp
        Pending = 1, //đang chờ duyệt
        Active = 2, //đang hoạt động
        Rejected = 3, //bị từ chối
        Closed = 4, //đã đóng thủ công hoặc do hệ thống
        Deleted = 5
    }
}
