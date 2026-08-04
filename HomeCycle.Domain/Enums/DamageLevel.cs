using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum DamageLevel
    {
        None = 0,
        Cosmetic_Damage = 1, //Hư hại thẩm mỹ. Vật dụng trầy xước, móp méo nhỏ bên ngoài. Máy vẫn chạy tốt
        Minor_Damage = 2, //Hư hại nhẹ. Hỏng hóc nhỏ, dễ thay thế
        Moderate_Damage = 3, //Hư hại trung bình. Bộ phận chính bị hỏng khiến máy ngừng hoạt động
        Severe_Damage = 4, //Hư hại nặng. Nhiều linh kiện hỏng cùng lúc hoặc cấu trúc biến dạng mạnh. Chi phí sửa gần bằng mua mới
        Total_Loss = 5 //Tổn thất toàn bộ. Vật dụng bị thiêu rụi, ngập nước hoàn toàn hoặc nát vụn. Không thể phục hồi

    }
}
