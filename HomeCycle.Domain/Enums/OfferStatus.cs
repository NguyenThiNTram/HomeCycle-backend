using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum OfferStatus
    {
        Pending = 0, //Offer vừa được gửi, đang chờ người nhận xử lý
        Accepted = 1, //Người nhận đã đồng ý offer. Giá và số lượng của offer được đưa vào bước thương lượng/thỏa thuận tiếp theo
        Rejected = 2, //Người nhận từ chối offer. Offer kết thúc và không được xử lý tiếp.
        Cancelled = 3, //Người gửi chủ động rút lại offer trước khi nó được chấp nhận
        Completed = 4, //Offer đã được hoàn tất (giao dịch thành công)
        Closed = 5, //Offer bị đóng do quy trình liên quan kết thúc nhưng không hoàn tất giao dịch, chẳng hạn negotiation bị đóng hoặc agreement bị hủy.
        Expired = 6 //Offer đã hết hạn (người nhận không phản hồi trong thời gian quy định)
    }
}
