using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum InputMode
    {
        OptionOnly = 1,     // Chỉ chọn từ danh sách Dropdown/Radio (Ví dụ: Tình trạng, Thương hiệu)
        CustomOnly = 2,     // Chỉ nhập tay dạng Text/Number (Ví dụ: Dung tích, Công suất, Kích thước)
        OptionOrCustom = 3  // Chọn từ Dropdown HOẶC nhập tay nếu chọn "Khác" (Ví dụ: Màu sắc)
    }
}
