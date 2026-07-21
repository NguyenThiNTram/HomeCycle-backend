using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum InputType
    {
        TextBox = 0,      // Ô nhập chữ tự do (Dành cho DataType = Text)
        NumberBox = 1,    // Ô nhập số chuyên dụng (Dành cho DataType = Number)
        CheckBox = 2,     // Nút tích chọn bật/tắt (Dành cho DataType = Boolean)
        Dropdown = 3,     // Danh sách chọn một giá trị (Yêu cầu phải chọn khớp OptionId)
        RadioButton = 4   // Nút chọn vòng tròn một giá trị duy nhất từ tập Option
    }
}
