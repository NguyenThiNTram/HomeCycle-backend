using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum MessageType
    {
        Text = 0,        // Tin nhắn văn bản
        Media = 1,       // Tin nhắn hình ảnh/file
        Offer = 2,       // Tin nhắn đề nghị giá
        CounterOffer = 3, // Đề nghị giá mới trong thương lượng
        System = 4       // Thông báo hệ thống
    }
}
