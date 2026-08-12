using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum ShipmentStatus
    {
        ReadyToPick = 1, //Đơn mới tạo, đang chờ shipper GHN đến kho lấy hàng
        Delivering = 2, //Shipper đã lấy hàng thành công và đang trên đường giao tới người mua
        Delivered = 3, //Người mua đã nhận hàng thành công và thanh toán tiền (nếu có)
        Cancelled = 4, //Đơn hàng đã bị hủy (do người mua hủy hoặc shop hủy trước khi lấy)
        Returning = 5, //Giao hàng thất bại (gọi khách không được), đang trên đường chuyển hoàn trả lại cho người bán
        Returned = 6, //Shipper đã mang hàng hoàn trả về tận tay người bán thành công
        Damage_Lost = 7, //Hàng hóa bị hư hỏng hoặc thất lạc trong quá trình vận chuyển (để xử lý đền bù)
        
        Exception = 8 // Ngoại lệ vận hành, chưa khẳng định hàng đã hư hỏng hoặc thất lạc
    }
}
