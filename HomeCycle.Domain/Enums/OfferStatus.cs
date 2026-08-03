using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum OfferStatus
    {
        Pending = 0, //Đang chờ người nhận phản hồi
        Accepted = 1, //Người nhận đã chấp nhận
        Rejected = 2, //Người nhận từ chối
        Cancelled = 3, //Người gửi hủy
        Expired = 4, //Hết hạn (người nhận không phản hồi trong thời gian quy định)
        Unavailable = 5 //Không còn đủ số lượng hoặc bài đăng không còn hoạt động
    }
}
