using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum DataType
    {
        Text = 1,    // Kiểu chuỗi ký tự tự do -> Ánh xạ vào cột ValueText
        Number = 2,  // Kiểu số (Kích thước, dung tích...) -> Ánh xạ vào cột ValueNumber
        Boolean = 3,  // Kiểu Có/Không (Ví dụ: Có Inverter không) -> Ánh xạ vào cột ValueBoolean
        Option = 4   // Kiểu lựa chọn từ danh sách (Ví dụ: Màu sắc, Chất liệu...) -> Ánh xạ vào cột OptionId
    }
}
