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
        //Pending = 1, //đang chờ duyệt
        Active = 1, //đang hoạt động
        Suspended = 2, //bị đình chỉ bài đăng vì vi phạm chính sách
        Closed = 3, //đã đóng thủ công hoặc do hệ thống
        Deleted = 4
    }
}
