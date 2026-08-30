using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum AppointmentStatus
    {
        Proposed = 0, // lịch đề xuất mới, đang chờ bên kia accept
        Scheduled = 1, // lịch chính thức đã được hai bên thống nhất
        Completed = 2, // nghiệp vụ của cuộc hẹn đã hoàn tất
        Cancelled = 3, // lịch bị hủy hoặc bị thay thế
        Expired = 4, // quá hạn nhưng cuộc hẹn không hoàn thành
        InProgress = 5 // cuộc hẹn đã thực sự bắt đầu
    }
}
