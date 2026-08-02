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
        CounterOffer = 2, // Đề nghị giá mới trong thương lượng
        System = 3       // Thông báo hệ thống
    }
}
