using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum MessageOfferStatus
    {
        //Trạng thái từng proposal giá/số lượng trong negotiation
        //Chỉ áp dụng khi MessageType == CounterOffer

        Pending = 0, //Đề nghị vừa được gửi (Offer ban đầu hoặc Counter Offer) và đang chờ bên kia phản hồi
        Accepted = 1, //Đối tác đã click "Chấp nhận" đề nghị này
        Rejected = 2, //Đối tác không đồng ý với mức giá/số lượng này và bấm "Từ chối"
        Superseded = 3 //Đề nghị này không còn giá trị do một trong hai bên cập nhật offer mới (Counter)
    }
}
