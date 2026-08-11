using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum GHNCreationStatus
    {
        Pending = 0, //Vận đơn vừa được tạo trên hệ thống - chưa gửi sang GHN
        Processing = 1, // Đang gửi request sang GHN
        Success = 2, // Đã gửi request sang GHN thành công, GHN đã tạo vận đơn - 200
        Failed = 3, // Đã gửi request sang GHN nhưng GHN trả về lỗi - 400, 500, 404, 403, 401
        Uncertain = 4 // GHN trả về trạng thái không xác định, cần kiểm tra lại
    }
}
